using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPurchaseRepository
{
    Task<Purchase?> GetByIdAsync(Guid id);
    Task<List<Purchase>> GetFilteredAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId);
    Task<List<Purchase>> GetPendingAsync(Guid branchId);
    Task<decimal> GetTotalPayableAsync(Guid branchId);
    Task<string> GeneratePurchaseNumberAsync(Guid companyId);
    Task AddAsync(Purchase purchase);
    Task UpdateAsync(Purchase purchase);
    Task SaveChangesAsync();
}
