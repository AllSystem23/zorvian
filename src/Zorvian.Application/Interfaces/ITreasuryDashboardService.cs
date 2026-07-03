namespace Zorvian.Application.Interfaces;

public sealed record TreasuryDashboardSummary(
    decimal TotalBankBalance,
    int PendingDeposits,
    int OutstandingChecks,
    int PendingReconciliations
);

public interface ITreasuryDashboardService
{
    Task<TreasuryDashboardSummary> GetSummaryAsync();
}
