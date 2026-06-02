using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByCodeAsync(string code, Guid branchId);
    Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid branchId);
    Task<List<Product>> GetLowStockAsync(Guid branchId);
    Task<List<Product>> GetOutOfStockAsync(Guid branchId);
    Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid branchId, int count);
    Task<int> GetTotalCountAsync(Guid branchId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Product product);
    Task SaveChangesAsync();
}
