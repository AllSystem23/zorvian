using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarehouseRepository : IWarehouseRepository
{
    private readonly ZorvianDbContext _db;

    public WarehouseRepository(ZorvianDbContext db) => _db = db;

    public async Task<Warehouse?> GetByIdAsync(Guid id) =>
        await _db.Warehouses
            .Include(w => w.Branch)
            .FirstOrDefaultAsync(w => w.Id == id);

    public async Task<Warehouse?> GetDefaultAsync(Guid branchId) =>
        await _db.Warehouses
            .Include(w => w.Branch)
            .Where(w => w.BranchId == branchId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .FirstOrDefaultAsync();

    public async Task<List<Warehouse>> GetByBranchAsync(Guid branchId) =>
        await _db.Warehouses
            .Include(w => w.Branch)
            .Where(w => w.BranchId == branchId)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync();

    public async Task<(List<Warehouse> Items, int Total)> GetFilteredAsync(
        string? search, Guid? branchId, bool? isActive, int page, int pageSize)
    {
        var query = _db.Warehouses
            .Include(w => w.Branch)
            .AsQueryable();

        if (branchId is { } b && b != Guid.Empty)
            query = query.Where(w => w.BranchId == b);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(w =>
                w.Name.ToLower().Contains(s) ||
                (w.Code != null && w.Code.ToLower().Contains(s)));
        }

        if (isActive.HasValue)
            query = query.Where(w => w.IsActive == isActive.Value);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public Task<bool> ExistsNameAsync(Guid branchId, string name, Guid? excludeId = null)
    {
        var lower = name.ToLower();
        return _db.Warehouses.AnyAsync(w =>
            w.BranchId == branchId &&
            w.Name.ToLower() == lower &&
            (excludeId == null || w.Id != excludeId));
    }

    public Task<int> CountByBranchAsync(Guid branchId) =>
        _db.Warehouses.CountAsync(w => w.BranchId == branchId);

    public Task<int> CountProductsAsync(Guid warehouseId) =>
        _db.Products.CountAsync(p => p.WarehouseId == warehouseId);

    public async Task AddAsync(Warehouse warehouse) =>
        await _db.Warehouses.AddAsync(warehouse);

    public Task UpdateAsync(Warehouse warehouse)
    {
        _db.Warehouses.Update(warehouse);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Warehouse warehouse)
    {
        _db.Warehouses.Remove(warehouse);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
