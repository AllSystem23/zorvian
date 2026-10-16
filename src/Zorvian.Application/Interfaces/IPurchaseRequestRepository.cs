using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPurchaseRequestRepository
{
    Task<PurchaseRequest?> GetByIdAsync(Guid id);
    Task<(List<PurchaseRequest> Items, int Total)> GetFilteredAsync(
        string? search, string? status, string? priority,
        Guid branchId, int page, int pageSize);
    Task<string> GenerateRequestNumberAsync(Guid companyId);
    Task AddAsync(PurchaseRequest request);
    Task SaveChangesAsync();
}
