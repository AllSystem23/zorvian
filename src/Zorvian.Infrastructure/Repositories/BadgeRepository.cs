using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BadgeRepository : IBadgeRepository
{
    private readonly ZorvianDbContext _db;
    private readonly Zorvian.Core.Interfaces.ITenantContext _tenant;

    public BadgeRepository(ZorvianDbContext db, Zorvian.Core.Interfaces.ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private bool NeedsBypass => _tenant.TenantId.Value == Guid.Empty;

    private IQueryable<T> Query<T>() where T : class
    {
        var set = _db.Set<T>().AsQueryable();
        return NeedsBypass ? set.IgnoreQueryFilters().Where(e => !EF.Property<bool>(e, "IsDeleted")) : set;
    }

    public async Task<int> GetCreditsPendingCountAsync()
    {
        return await Query<Credit>().CountAsync(c => c.Status == "active" || c.Status == "pending");
    }

    public async Task<int> GetOverdueCreditsCountAsync()
    {
        return await Query<CreditInstallment>()
            .CountAsync(i => i.DueDate < DateOnly.FromDateTime(DateTime.UtcNow) && i.Status == "pending");
    }

    public async Task<int> GetWarrantiesPendingCountAsync()
    {
        return await Query<Warranty>().CountAsync(w =>
            w.Status == WarrantyStatus.PendingReview ||
            w.Status == WarrantyStatus.InDiagnosis ||
            w.Status == WarrantyStatus.InRepair ||
            w.Status == WarrantyStatus.PendingParts);
    }

    public async Task<int> GetApprovalsPendingCountAsync()
    {
        return await Query<ApprovalRequest>().CountAsync(a => a.Status == "pending");
    }
}
