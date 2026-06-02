using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly NexoraDbContext _db;

    public InventoryMovementRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<InventoryMovement?> GetByIdAsync(Guid id) =>
        await _db.Set<InventoryMovement>()
            .Include(m => m.Product)
            .Include(m => m.PerformedBy)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<InventoryMovement>()
            .Include(m => m.Product)
            .Include(m => m.PerformedBy)
            .Where(m => m.BranchId == branchId)
            .AsQueryable();

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);
        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, Guid branchId)
    {
        var query = _db.Set<InventoryMovement>().Where(m => m.BranchId == branchId).AsQueryable();

        if (productId.HasValue) query = query.Where(m => m.ProductId == productId.Value);
        if (!string.IsNullOrWhiteSpace(movementType)) query = query.Where(m => m.MovementType == movementType);
        if (fromDate.HasValue) query = query.Where(m => m.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(m => m.CreatedAt <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task AddAsync(InventoryMovement movement) =>
        await _db.Set<InventoryMovement>().AddAsync(movement);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
