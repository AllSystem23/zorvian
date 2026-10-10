using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Jobs;

public sealed class WarrantySlaMonitorJob
{
    private readonly IWarrantyRepository _warrantyRepo;
    private readonly INotificationService _notifier;
    private readonly IAuthRepository _authRepo;
    private readonly ITenantContextWriter _tenantWriter;

    public WarrantySlaMonitorJob(
        IWarrantyRepository warrantyRepo,
        INotificationService notifier,
        IAuthRepository authRepo,
        ITenantContextWriter tenantWriter)
    {
        _warrantyRepo = warrantyRepo;
        _notifier = notifier;
        _authRepo = authRepo;
        _tenantWriter = tenantWriter;
    }

    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;

        // Sin request HTTP los query filters no ven ninguna compañía → iterar por
        // empresa y fijar el tenant antes de leer las garantías en riesgo.
        var companies = await _authRepo.GetAllCompaniesAsync();

        foreach (var company in companies)
        {
            _tenantWriter.SetTenantId(company.TenantId);

            var atRiskWarranties = await _warrantyRepo.GetAtRiskWarrantiesAsync();

            foreach (var w in atRiskWarranties)
            {
                if (w.SlaDueAt.HasValue && now > w.SlaDueAt.Value)
                {
                    // SLA Breached
                    w.SlaBreachedAt = now;
                    await _notifier.NotifyTenantAsync(w.CompanyId.ToString(), "SLA Excedido", $"SLA Excedido para Garantía {w.WarrantyNumber}", "SLA_BREACHED", w.Id.ToString());
                }
            }
            await _warrantyRepo.SaveChangesAsync();
        }
    }
}
