using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Barrido de logos huérfanos: recorre los objetos bajo <c>companies/</c> y
/// borra los que ninguna compañía referencia.
///
/// Los borrados "en el momento" (reemplazo, quitar logo) ya cubren el caso
/// normal; este job es la red de seguridad para huérfanos que no se pueden
/// borrar en línea: procesos interrumpidos entre la subida y el guardado,
/// archivos de una compañía eliminada y residuos de una migración de formato.
///
/// Deliberadamente NO toca otras prefijos (<c>attendance/</c>,
/// <c>permissions/</c>, documentos generados) porque sus dueños son otras
/// entidades y cada uno tiene su propia política de retención.
/// </summary>
public sealed class CompanyLogoOrphanSweepJob
{
    /// <summary>Prefijo exclusivo de los logos de compañía.</summary>
    public const string LogoPrefix = "companies/";

    /// <summary>
    /// Ventana de gracia: una subida recién hecha puede no estar aún
    /// referenciada (subida + guardado no son atómicos), así que no se borra
    /// nada más nuevo que esto.
    /// </summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

    private readonly ZorvianDbContext _db;
    private readonly IDocumentStorageService _storage;
    private readonly ILogger<CompanyLogoOrphanSweepJob> _logger;

    public CompanyLogoOrphanSweepJob(
        ZorvianDbContext db,
        IDocumentStorageService storage,
        ILogger<CompanyLogoOrphanSweepJob> logger)
    {
        _db = db;
        _storage = storage;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        IReadOnlyList<StorageObjectInfo> objects;
        try
        {
            objects = await _storage.ListFilesAsync(LogoPrefix);
        }
        catch (Exception ex)
        {
            // Sin listado (credenciales, bucket inexistente) no hay nada que
            // hacer: se registra y se espera al siguiente ciclo.
            _logger.LogWarning(ex, "Barrido de logos: no se pudo listar {Prefix}", LogoPrefix);
            return;
        }

        if (objects.Count == 0) return;

        // IgnoreQueryFilters: el logo de una compañía desactivada o
        // soft-deleted SIGUE referenciado y no debe borrarse.
        var referencedValues = await _db.Companies
            .IgnoreQueryFilters()
            .Select(c => c.LogoUrl)
            .ToListAsync();

        var referenced = referencedValues
            .Select(StoragePathHelper.ExtractStoragePath)
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => p!)
            .ToHashSet(StringComparer.Ordinal);

        var cutoff = DateTimeOffset.UtcNow - GracePeriod;
        var deleted = 0;

        foreach (var obj in objects)
        {
            if (referenced.Contains(obj.Path)) continue;
            if (obj.LastModifiedUtc > cutoff) continue;

            try
            {
                await _storage.DeleteFileAsync(obj.Path);
                deleted++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Barrido de logos: no se pudo borrar {Path}", obj.Path);
            }
        }

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Barrido de logos: {Deleted} huérfano(s) borrado(s) de {Total} objeto(s) en {Prefix}",
                deleted, objects.Count, LogoPrefix);
        }
    }
}
