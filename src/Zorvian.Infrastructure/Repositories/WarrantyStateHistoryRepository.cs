using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyStateHistoryRepository : IWarrantyStateHistoryRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyStateHistoryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyStateHistory>> GetByWarrantyIdAsync(Guid warrantyId) =>
        await _db.Set<WarrantyStateHistory>()
            .Where(h => h.WarrantyId == warrantyId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

    public async Task<List<WarrantyStateHistory>> GetByClaimIdAsync(Guid claimId) =>
        await _db.Set<WarrantyStateHistory>()
            .Where(h => h.ClaimId == claimId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

    public async Task AddAsync(WarrantyStateHistory history) =>
        await _db.Set<WarrantyStateHistory>().AddAsync(history);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
