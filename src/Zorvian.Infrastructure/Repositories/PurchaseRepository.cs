using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PurchaseRepository : IPurchaseRepository
{
    private readonly ZorvianDbContext _db;

    public PurchaseRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Purchase?> GetByIdAsync(Guid id) =>
        await _db.Set<Purchase>()
            .Include(p => p.Supplier)
            .Include(p => p.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<List<Purchase>> GetFilteredAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Purchase>()
            .Include(p => p.Supplier)
            .Where(p => p.BranchId == branchId)
            .AsQueryable();

        if (supplierId.HasValue)
            query = query.Where(p => p.SupplierId == supplierId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);
        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId)
    {
        var query = _db.Set<Purchase>().Where(p => p.BranchId == branchId).AsQueryable();

        if (supplierId.HasValue) query = query.Where(p => p.SupplierId == supplierId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(p => p.Status == status);
        if (fromDate.HasValue) query = query.Where(p => p.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(p => p.CreatedAt <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<string> GeneratePurchaseNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Purchase>().CountAsync(p => p.CompanyId == companyId);
        return $"COMP-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task AddAsync(Purchase purchase) =>
        await _db.Set<Purchase>().AddAsync(purchase);

    public Task UpdateAsync(Purchase purchase)
    {
        _db.Set<Purchase>().Update(purchase);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
