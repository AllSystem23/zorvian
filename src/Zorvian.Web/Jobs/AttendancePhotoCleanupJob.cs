using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Job que limpia fotos de asistencia antiguas para optimizar almacenamiento.
/// </summary>
public sealed class AttendancePhotoCleanupJob
{
    private readonly ZorvianDbContext _db;
    private readonly IDocumentStorageService _storage;
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public AttendancePhotoCleanupJob(
        ZorvianDbContext db,
        IDocumentStorageService storage,
        IAuthRepository authRepo,
        ITenantContextWriter tenantWriter)
    {
        _db = db;
        _storage = storage;
        _authRepo = authRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task RunAsync()
    {
        // Retención de 90 días
        var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-90));

        // Fondo sin request HTTP → tenant en GUID-cero; iterar por compañía para
        // que los query filters devuelvan los registros reales (patrón VacationAutomatedJob).
        var companies = await _authRepo.GetAllCompaniesAsync();

        foreach (var company in companies)
        {
            _tenantWriter.SetTenantId(company.TenantId);

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
