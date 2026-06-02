using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly ZorvianDbContext _db;

    public ProductRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Product?> GetByIdAsync(Guid id) =>
        await _db.Set<Product>()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Product?> GetByCodeAsync(string code, Guid branchId) =>
        await _db.Set<Product>()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Code == code && p.BranchId == branchId);

    public async Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Product>()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Supplier)
            .Where(p => p.BranchId == branchId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Code.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.Contains(s)));
        }

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
        if (lowStock == true) query = query.Where(p => p.Stock <= p.MinStock);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);

        return await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid branchId)
    {
        var query = _db.Set<Product>().Where(p => p.BranchId == branchId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(s) ||
                p.Code.ToLower().Contains(s) ||
                (p.Barcode != null && p.Barcode.Contains(s)));
        }

        if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
        if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);
        if (lowStock == true) query = query.Where(p => p.Stock <= p.MinStock);
        if (isActive.HasValue) query = query.Where(p => p.IsActive == isActive.Value);

        return await query.CountAsync();
    }

    public async Task<List<Product>> GetLowStockAsync(Guid branchId) =>
        await _db.Set<Product>()
            .Include(p => p.Category)
            .Where(p => p.BranchId == branchId && p.Stock <= p.MinStock && p.IsActive)
            .OrderBy(p => p.Stock)
            .ToListAsync();

    public async Task<List<Product>> GetOutOfStockAsync(Guid branchId) =>
        await _db.Set<Product>()
            .Include(p => p.Category)
            .Where(p => p.BranchId == branchId && p.Stock <= 0 && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

    public async Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid branchId, int count)
    {
        var topProducts = await _db.Set<SaleDetail>()
            .Where(d => d.BranchId == branchId)
            .GroupBy(d => new { d.ProductId, d.Product!.Name })
            .OrderByDescending(g => g.Sum(d => d.Quantity))
            .Select(g => new { g.Key.ProductId, g.Key.Name, TotalSold = g.Sum(d => d.Quantity) })
            .Take(count)
            .ToListAsync();

        var productIds = topProducts.Select(t => t.ProductId).ToList();
        var products = await _db.Set<Product>()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        return topProducts
            .Join(products, t => t.ProductId, p => p.Id, (t, p) => (p, t.TotalSold))
            .ToList();
    }

    public async Task<int> GetTotalCountAsync(Guid branchId) =>
        await _db.Set<Product>().CountAsync(p => p.BranchId == branchId && p.IsActive);

    public async Task AddAsync(Product product) =>
        await _db.Set<Product>().AddAsync(product);

    public Task UpdateAsync(Product product)
    {
        _db.Set<Product>().Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Product product)
    {
        _db.Set<Product>().Remove(product);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
