using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Infrastructure.Data;

namespace Nexora.Web.Jobs;

/// <summary>
/// Job que limpia fotos de asistencia antiguas para optimizar almacenamiento.
/// </summary>
public sealed class AttendancePhotoCleanupJob
{
    private readonly NexoraDbContext _db;
    private readonly IDocumentStorageService _storage;

    public AttendancePhotoCleanupJob(NexoraDbContext db, IDocumentStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task RunAsync()
    {
        // Retención de 90 días
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));

        var oldRecords = await _db.AttendanceRecords
            .Where(r => r.Date < cutoffDate && (r.CheckInPhotoUrl != null || r.CheckOutPhotoUrl != null))
            .ToListAsync();

        foreach (var record in oldRecords)
        {
            if (!string.IsNullOrEmpty(record.CheckInPhotoUrl))
            {
                await DeletePhotoAsync(record.CheckInPhotoUrl);
                record.CheckInPhotoUrl = null;
            }

            if (!string.IsNullOrEmpty(record.CheckOutPhotoUrl))
            {
                await DeletePhotoAsync(record.CheckOutPhotoUrl);
                record.CheckOutPhotoUrl = null;
            }
        }

        if (oldRecords.Count > 0)
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task DeletePhotoAsync(string url)
    {
        // Extraer el path relativo de la URL de Firebase
        // Formato: https://storage.googleapis.com/{bucket}/{path}
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // El primer segmento es el bucket, los demás son el path
            if (segments.Length > 1)
            {
                var path = string.Join("/", segments.Skip(1));
                await _storage.DeleteFileAsync(path);
            }
        }
        catch
        {
            // Ignorar errores al parsear URL para no detener el job
        }
    }
}
