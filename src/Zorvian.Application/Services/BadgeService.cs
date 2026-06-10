using Zorvian.Application.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BadgeService : IBadgeService
{
    private readonly IBadgeRepository _repo;

    public BadgeService(IBadgeRepository repo)
    {
        _repo = repo;
    }

    public async Task<Dictionary<string, int>> GetAllAsync()
    {
        return new Dictionary<string, int>
        {
            { "credits-pending", await _repo.GetCreditsPendingCountAsync() },
            { "overdue-credits", await _repo.GetOverdueCreditsCountAsync() },
            { "warranties-pending", await _repo.GetWarrantiesPendingCountAsync() },
            { "approvals-pending", await _repo.GetApprovalsPendingCountAsync() },
        };
    }
}
