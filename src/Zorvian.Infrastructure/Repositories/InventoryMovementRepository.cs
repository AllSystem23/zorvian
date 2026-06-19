using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly ZorvianDbContext _db;

    public InventoryMovementRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryMovement?> GetByIdAsync(Guid id) =>
        await _db.Set<InventoryMovement>()
            .Include(m => m.Product)
            .Include(m => m.PerformedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId, int page, int pageSize)
    {
        var query = _db.Set<InventoryMovement>()
            .Include(m => m.Product)
            .Include(m => m.PerformedBy)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(m => m.BranchId == branchId.Value);

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);
        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(m => m.ReferenceNumber != null && m.ReferenceNumber.ToLower().Contains(q)
                || m.Product.Name.ToLower().Contains(q)
                || m.Product.Code.ToLower().Contains(q));
        }

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId)
    {
        var query = _db.Set<InventoryMovement>()
            .Include(m => m.Product)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(m => m.BranchId == branchId.Value);

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);
        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(m => m.ReferenceNumber != null && m.ReferenceNumber.ToLower().Contains(q)
                || m.Product.Name.ToLower().Contains(q)
                || m.Product.Code.ToLower().Contains(q));
        }

        return await query.CountAsync();
    }

    public async Task AddAsync(InventoryMovement movement) =>
        await _db.Set<InventoryMovement>().AddAsync(movement);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
