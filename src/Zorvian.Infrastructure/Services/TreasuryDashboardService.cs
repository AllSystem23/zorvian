using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class TreasuryDashboardService : ITreasuryDashboardService
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public TreasuryDashboardService(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<TreasuryDashboardSummary> GetSummaryAsync()
    {
        var companyId = _tenant.EffectiveCompanyId;

        // Total bank balance from corporate BankAccounts (has CurrentBalance field)
        var totalBankBalance = await _db.BankAccounts
            .Where(ba => (!companyId.HasValue || ba.CompanyId == companyId.Value) && ba.IsActive)
            .SumAsync(ba => (decimal?)ba.CurrentBalance) ?? 0m;

        // Pending reconciliations (not completed)
        var pendingReconciliations = await _db.Reconciliations
            .Where(r => (!companyId.HasValue || r.CompanyId == companyId.Value) && r.Status != "completed")
            .CountAsync();

        // Outstanding checks (not Draft and not completed/cancelled)
        var outstandingChecks = await _db.Checks
            .Where(c => (!companyId.HasValue || c.CompanyId == companyId.Value) &&
                        c.Status != Zorvian.Core.Entities.CheckStatus.Draft &&
                        c.Status != Zorvian.Core.Entities.CheckStatus.Cashed &&
                        c.Status != Zorvian.Core.Entities.CheckStatus.Cancelled)
            .CountAsync();

        // Pending deposits (CashMovements of type "deposit" not yet approved)
        var pendingDeposits = await _db.CashMovements
            .Where(cm => (!companyId.HasValue || cm.CompanyId == companyId.Value) &&
                         cm.MovementType == "deposit" &&
                         cm.ApprovalStatus != "approved")
            .CountAsync();

        return new TreasuryDashboardSummary(totalBankBalance, pendingDeposits, outstandingChecks, pendingReconciliations);
    }
}
