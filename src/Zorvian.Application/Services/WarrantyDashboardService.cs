using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyDashboardService
{
    private readonly IWarrantyRepository _repo;
    private readonly Zorvian.Core.Interfaces.ITenantContext _tenant;

    public WarrantyDashboardService(IWarrantyRepository repo, Zorvian.Core.Interfaces.ITenantContext tenant)
    {
        _repo = repo;
        _tenant = tenant;
    }

    /// <summary>
    /// All 6 dashboard counts in a single raw SQL round-trip (no parallel needed).
    /// Supports SuperAdmin bypass and specific tenant filtering.
    /// </summary>
    public async Task<WarrantyDashboardResponse> GetDashboardMetricsAsync()
    {
        var tenantId = _tenant.TenantId.Value.ToString();
        var isSuperAdmin = _tenant.BypassTenantFilter;
        // Sucursal activa del header (X-Branch-Id); Guid.Empty = todas las sucursales.
        var branchId = _tenant.ResolveBranchId() ?? Guid.Empty;

        var scalars = await _repo.GetDashboardScalarsRawAsync(tenantId, isSuperAdmin, branchId);

        return new WarrantyDashboardResponse(
            TotalActive: scalars.TotalActive,
            TotalBreachedSla: scalars.TotalBreachedSla,
            RegisteredCount: scalars.RegisteredCount,
            InDiagnosisCount: scalars.InDiagnosisCount,
            InRepairCount: scalars.InRepairCount,
            ReadyForDeliveryCount: scalars.ReadyForDeliveryCount);
    }
}
