using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

/// <summary>
/// Job que verifica diariamente los documentos de empleados que están por vencer
/// y envía notificaciones a los interesados.
/// </summary>
public sealed class DocumentExpirationJob
{
    private readonly ZorvianDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public DocumentExpirationJob(
        ZorvianDbContext db,
        INotificationService notificationService,
        IAuthRepository authRepo,
        ITenantContextWriter tenantWriter)
    {
        _db = db;
        _notificationService = notificationService;
        _authRepo = authRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thresholds = new[] { 30, 15, 7, 1 };

        // Fondo sin request HTTP → tenant en GUID-cero; iterar por compañía para
        // que los query filters devuelvan los documentos reales (patrón VacationAutomatedJob).
        var companies = await _authRepo.GetAllCompaniesAsync();

        foreach (var company in companies)
        {
            _tenantWriter.SetTenantId(company.TenantId);

            foreach (var days in thresholds)
            {
                var targetDate = today.AddDays(days);

                var expiringDocs = await _db.EmployeeDocuments
                    .Include(d => d.Employee)
                    .Where(d => d.ExpiryDate == targetDate)
                    .ToListAsync();

                foreach (var doc in expiringDocs)
                {
                    var title = "Documento por vencer";
                    var message = $"El documento '{doc.DocumentType}' de {doc.Employee.FirstName} {doc.Employee.LastName} vence el {doc.ExpiryDate}.";

                    // Notificar al empleado (si tiene usuario asociado)
                    if (doc.Employee.UserId.HasValue)
                    {
                        await _notificationService.NotifyUserAsync(
                            doc.TenantId,
                            doc.Employee.UserId.Value.ToString(),
                            title,
                            message,
                            "document_expiration",
                            doc.Id.ToString()
                        );
                    }

                    // Notificar a RRHH de la empresa
                    await _notificationService.NotifyTenantAsync(
                        doc.TenantId,
                        "Alerta de Vencimiento",
                        message,
                        "document_expiration_admin",
                        doc.Id.ToString()
                    );
                }
            }
        }
    }
}
