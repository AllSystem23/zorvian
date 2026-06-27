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
            .Include(p => p.TaxCategory)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Product?> GetByCodeAsync(string code, Guid branchId) =>
        await _db.Set<Product>()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Supplier)
            .Include(p => p.TaxCategory)
            .FirstOrDefaultAsync(p => p.Code == code && p.BranchId == branchId);

    public async Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId, int page, int pageSize)
    {
        var query = _db.Set<Product>()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Supplier)
            .Include(p => p.TaxCategory)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);

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

    public async Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId)
    {
        var query = _db.Set<Product>().AsQueryable();

        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);

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

    public async Task<List<Product>> GetLowStockAsync(Guid? branchId)
    {
        var query = _db.Set<Product>()
            .Include(p => p.Category)
            .Where(p => p.Stock <= p.MinStock && p.IsActive)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);

        return await query
            .OrderBy(p => p.Stock)
            .ToListAsync();
    }

    public async Task<List<Product>> GetOutOfStockAsync(Guid? branchId)
    {
        var query = _db.Set<Product>()
            .Include(p => p.Category)
            .Where(p => p.Stock <= 0 && p.IsActive)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid? branchId, int count)
    {
        var query = _db.Set<SaleDetail>()
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(d => d.BranchId == branchId.Value);

        var topProducts = await query
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

    public async Task<InventorySummaryRaw> GetInventorySummaryRawAsync(Guid? branchId)
    {
        var query = _db.Set<Product>()
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (branchId.HasValue)
            query = query.Where(p => p.BranchId == branchId.Value);

        var totals = await query
            .Select(p => new
            {
                TotalValue = p.CostPrice * p.Stock,
                Stock = p.Stock,
                LowStock = p.Stock <= p.MinStock ? 1 : 0,
                OutOfStock = p.Stock <= 0 ? 1 : 0
            })
            .GroupBy(_ => 1)
            .Select(g => new InventorySummaryRaw(
                g.Sum(x => x.TotalValue),
                g.Count(),
                g.Sum(x => x.LowStock),
                g.Sum(x => x.OutOfStock),
                g.Count() > 0 ? (double)g.Sum(x => x.Stock) / g.Count() : 0,
                new List<InventoryCategoryRaw>(),
                new List<InventorySlowMoverRaw>()))
            .SingleOrDefaultAsync();

        var raw = await query
            .Select(p => new
            {
                CategoryName = p.Category != null ? p.Category.Name : null,
                p.CostPrice,
                p.SellingPrice,
                p.Stock
            })
            .ToListAsync();

        var categories = raw
            .GroupBy(p => p.CategoryName ?? "Sin categoría")
            .Select(g => new InventoryCategoryRaw(
                g.Key,
                g.Count(),
                g.Sum(p => p.CostPrice * p.Stock),
                g.Sum(p => p.SellingPrice * p.Stock)))
            .OrderByDescending(c => c.TotalCost)
            .ToList();

        var slowMovers = await _db.Database.SqlQueryRaw<InventorySlowMoverRaw>(@"
            SELECT
                p.""Name"" AS ""ProductName"",
                p.""Stock"",
                COALESCE((
                    SELECT MAX(im.""CreatedAt"")
                    FROM ""InventoryMovements"" im
                    WHERE im.""ProductId"" = p.""Id""
                      AND (@emptyBranch = true OR im.""BranchId"" = @branchId)
                      AND im.""IsDeleted"" = false
                ), TIMESTAMP '0001-01-01') AS ""LastMovement""
            FROM ""Products"" p
            WHERE (@emptyBranch = true OR p.""BranchId"" = @branchId)
              AND p.""IsDeleted"" = false
              AND p.""Stock"" > 0
            ORDER BY ""LastMovement"" ASC
            LIMIT 10",
            new Npgsql.NpgsqlParameter("@branchId", branchId ?? Guid.Empty),
            new Npgsql.NpgsqlParameter("@emptyBranch", !branchId.HasValue))
            .ToListAsync();

        return totals ?? new InventorySummaryRaw(0, 0, 0, 0, 0, categories, slowMovers) with
        {
            ByCategory = categories,
            TopSlowMovers = slowMovers
        };
    }

    public async Task<int> GetTotalCountAsync(Guid? branchId)
    {
        var query = _db.Set<Product>().Where(p => p.IsActive).AsQueryable();
        if (branchId.HasValue) query = query.Where(p => p.BranchId == branchId.Value);
        return await query.CountAsync();
    }

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
