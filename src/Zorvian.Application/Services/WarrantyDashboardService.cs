using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Enums;

namespace Zorvian.Application.Services;

public sealed class WarrantyDashboardService
{
    private readonly IWarrantyRepository _repo;

    public WarrantyDashboardService(IWarrantyRepository repo)
    {
        _repo = repo;
    }

    public async Task<WarrantyDashboardResponse> GetDashboardMetricsAsync()
    {
        return new WarrantyDashboardResponse(
            TotalActive: await _repo.GetFilteredCountAsync(null, null, null, Guid.Empty),
            TotalBreachedSla: await _repo.GetBreachedSlaCountAsync(),
            RegisteredCount: await _repo.GetCountByStatusAsync(WarrantyStatus.Registered.ToDbValue()),
            InDiagnosisCount: await _repo.GetCountByStatusAsync(WarrantyStatus.InDiagnosis.ToDbValue()),
            InRepairCount: await _repo.GetCountByStatusAsync(WarrantyStatus.InRepair.ToDbValue()),
            ReadyForDeliveryCount: await _repo.GetCountByStatusAsync(WarrantyStatus.ReadyForDelivery.ToDbValue())
        );
    }
}
