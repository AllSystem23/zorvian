using System.Text;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Backfill del HTML de las versiones de documento hacia Storage.
///
/// El renderizado de cada versión pesa ~40-80 KB y hasta ahora vivía en la columna
/// <c>DocumentVersions.Content</c>, una de las principales consumidoras de espacio
/// (Neon corta la BD al llegar a su límite de 1024 MB). Desde el cambio de escritura
/// las versiones nuevas suben el HTML a <c>documents/{docId}/vN.html</c> y dejan la
/// columna vacía; este job migra las filas ANTIGUAS.
///
/// Diseño: lotes de <see cref="BatchSize"/> filas por corrida, idempotente y
/// reanudable — una fila solo se vacía DESPUÉS de subirla con éxito, así que un
/// fallo de Storage (o un proceso interrumpido) la deja intacta y el siguiente
/// ciclo la reintenta. Si el lote sale completo se encadena otra corrida para
/// drenar la cola sin esperar al próximo cron.
///
/// Deliberadamente NO borra nada: la limpieza de espacio en Neon la hace
/// <c>VACUUM</c> cuando la columna quede vacía.
/// </summary>
public sealed class DocumentVersionContentSweepJob
{
    /// <summary>Filas migradas por corrida.</summary>
    public const int BatchSize = 50;

    /// <summary>Ruta por defecto para filas sin FilePath (formato del generador).</summary>
    public static string DefaultPath(Guid documentId, int versionNumber)
        => $"documents/{documentId}/v{versionNumber}.html";

    private readonly ZorvianDbContext _db;
    private readonly IDocumentStorageService _storage;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<DocumentVersionContentSweepJob> _logger;

    public DocumentVersionContentSweepJob(
        ZorvianDbContext db,
        IDocumentStorageService storage,
        IBackgroundJobClient backgroundJobs,
        ILogger<DocumentVersionContentSweepJob> logger)
    {
        _db = db;
        _storage = storage;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        // IgnoreQueryFilters: el job es global por diseño (mover un archivo entre
        // Storage y la BD no depende del tenant, igual que AttendancePhotoCleanupJob
        // y el barrido de logos). Sin tenant resuelto las políticas RLS no acotan.
        var pending = await _db.DocumentVersions
            .IgnoreQueryFilters()
            .Where(v => v.Content != null && v.Content != "")
            .OrderBy(v => v.CreatedAt)
            .Take(BatchSize)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        var migrated = 0;

        foreach (var version in pending)
        {
            var path = string.IsNullOrWhiteSpace(version.FilePath)
                ? DefaultPath(version.DocumentId, version.VersionNumber)
                : version.FilePath;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(version.Content ?? string.Empty);
                await using var stream = new MemoryStream(bytes);

                var storedPath = await _storage.UploadFileAsync(
                    stream, path, "text/html; charset=utf-8");

                if (string.IsNullOrEmpty(storedPath))
                {
                    // Sin ruta no hay dónde leerlo después: NO se vacía la fila
                    // (se reintenta en la próxima corrida).
                    _logger.LogWarning(
                        "Migración de HTML: el almacenamiento no devolvió la ruta de {Path}", path);
                    continue;
                }

                version.FilePath = storedPath;
                version.FileSizeBytes = bytes.LongLength;
                version.Content = string.Empty;   // ← esto es lo que libera el espacio
                migrated++;
            }
            catch (Exception ex)
            {
                // La fila conserva su contenido: se reintenta en la próxima corrida.
                _logger.LogWarning(ex,
                    "Migración de HTML: no se pudo subir la versión {VersionId} ({Path})",
                    version.Id, path);
            }
        }

        if (migrated > 0)
            await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Migración de HTML de versiones: {Migrated} de {Total} subidas a Storage.",
            migrated, pending.Count);

        // Lote completo → encadenar para drenar sin esperar al próximo cron.
        // Terminación garantizada: cada corrida vacía Content, así que la cola
        // decrece; si algo falla no se encadena y se reintenta en el próximo ciclo.
        if (migrated == pending.Count && pending.Count == BatchSize)
            _backgroundJobs.Enqueue(() => RunAsync());
    }
}
