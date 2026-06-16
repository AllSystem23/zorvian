using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyDashboardService
{
    private readonly IWarrantyRepository _repo;

    public WarrantyDashboardService(IWarrantyRepository repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// All 6 dashboard counts in a single raw SQL round-trip (no parallel needed).
    /// </summary>
    public async Task<WarrantyDashboardResponse> GetDashboardMetricsAsync()
    {
        var scalars = await _repo.GetDashboardScalarsRawAsync();

        return new WarrantyDashboardResponse(
            TotalActive: scalars.TotalActive,
            TotalBreachedSla: scalars.TotalBreachedSla,
            RegisteredCount: scalars.RegisteredCount,
            InDiagnosisCount: scalars.InDiagnosisCount,
            InRepairCount: scalars.InRepairCount,
            ReadyForDeliveryCount: scalars.ReadyForDeliveryCount);
    }
}
