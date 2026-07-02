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
    Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId);
    Task<List<Product>> GetLowStockAsync(Guid? branchId);
    Task<List<Product>> GetOutOfStockAsync(Guid? branchId);
    Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid? branchId, int count);
    Task<int> GetTotalCountAsync(Guid? branchId);
    Task<InventorySummaryRaw> GetInventorySummaryRawAsync(Guid? branchId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task SaveChangesAsync();
}
