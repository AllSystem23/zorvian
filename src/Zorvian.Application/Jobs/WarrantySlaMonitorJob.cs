using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Jobs;

public sealed class WarrantySlaMonitorJob
{
    private readonly IWarrantyRepository _warrantyRepo;
    private readonly INotificationService _notifier;

    public WarrantySlaMonitorJob(IWarrantyRepository warrantyRepo, INotificationService notifier)
    {
        _warrantyRepo = warrantyRepo;
        _notifier = notifier;
    }

    public async Task RunAsync()
    {
        var atRiskWarranties = await _warrantyRepo.GetAtRiskWarrantiesAsync();
        var now = DateTime.UtcNow;

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
