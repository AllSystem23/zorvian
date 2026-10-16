using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public sealed record InventorySummaryRaw(
    decimal TotalValue,
    int TotalProducts,
    int LowStockCount,
    int OutOfStockCount,
    double TurnoverRate,
    List<InventoryCategoryRaw> ByCategory,
    List<InventorySlowMoverRaw> TopSlowMovers
);

public sealed record InventoryCategoryRaw(
    string CategoryName,
    int Count,
    decimal TotalCost,
    decimal TotalValue
);

public sealed class InventorySlowMoverRaw
{
    public string ProductName { get; set; } = string.Empty;
    public int Stock { get; set; }
    public DateTime LastMovement { get; set; }
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByIdForUpdateAsync(Guid id);
    Task<Product?> GetByCodeAsync(string code, Guid branchId);

    /// <summary>Producto (fila de stock) por código DENTRO de una bodega concreta.</summary>
    Task<Product?> GetByCodeForWarehouseAsync(string code, Guid warehouseId);
    Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId, Guid? warehouseId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId, Guid? warehouseId);
    Task<List<Product>> GetLowStockAsync(Guid? branchId, Guid? warehouseId);
    Task<List<Product>> GetOutOfStockAsync(Guid? branchId);
    Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid? branchId, int count);
    Task<int> GetTotalCountAsync(Guid? branchId);
    Task<InventorySummaryRaw> GetInventorySummaryRawAsync(Guid? branchId, Guid? warehouseId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task SaveChangesAsync();
}
