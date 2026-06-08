using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IInventoryMovementRepository
{
    Task<InventoryMovement?> GetByIdAsync(Guid id);
    Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId);
    Task AddAsync(InventoryMovement movement);
    Task SaveChangesAsync();
}
