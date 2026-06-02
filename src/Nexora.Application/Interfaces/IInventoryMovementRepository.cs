using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IInventoryMovementRepository
{
    Task<InventoryMovement?> GetByIdAsync(Guid id);
    Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, Guid branchId);
    Task AddAsync(InventoryMovement movement);
    Task SaveChangesAsync();
}
