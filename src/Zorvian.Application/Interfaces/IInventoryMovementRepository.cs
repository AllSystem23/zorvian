using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IInventoryMovementRepository
{
    Task<InventoryMovement?> GetByIdAsync(Guid id);

    /// <summary>
    /// <paramref name="warehouseId"/> filtra por la bodega del producto
    /// (Product.WarehouseId); null = todas las bodegas.
    /// </summary>
    Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId, Guid? warehouseId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId, Guid? warehouseId);
    Task AddAsync(InventoryMovement movement);
    Task SaveChangesAsync();
}
