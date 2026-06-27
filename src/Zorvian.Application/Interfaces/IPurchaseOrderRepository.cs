using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder?> GetByIdAsync(Guid id);
    Task<(List<PurchaseOrder> Items, int Total)> GetFilteredAsync(
        string? search, Guid? supplierId, string? status,
        DateTime? fromDate, DateTime? toDate, Guid branchId,
        int page, int pageSize);
    Task<string> GenerateOrderNumberAsync(Guid companyId);
    Task AddAsync(PurchaseOrder order);
    Task UpdateAsync(PurchaseOrder order);
    Task SaveChangesAsync();
}
