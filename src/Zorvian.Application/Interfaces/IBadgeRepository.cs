namespace Zorvian.Application.Interfaces;

public interface IBadgeRepository
{
    Task<int> GetCreditsPendingCountAsync();
    Task<int> GetOverdueCreditsCountAsync();
    Task<int> GetWarrantiesPendingCountAsync();
    Task<int> GetApprovalsPendingCountAsync();
}
