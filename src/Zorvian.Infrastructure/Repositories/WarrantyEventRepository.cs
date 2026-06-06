using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyEventRepository : IWarrantyEventRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyEventRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyEvent>> GetByWarrantyIdAsync(Guid warrantyId) =>
        await _db.Set<WarrantyEvent>()
            .Where(e => e.WarrantyId == warrantyId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync();

    public async Task<List<WarrantyEvent>> GetMilestonesAsync(Guid warrantyId) =>
        await _db.Set<WarrantyEvent>()
            .Where(e => e.WarrantyId == warrantyId && e.IsMilestone)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync();

    public async Task AddAsync(WarrantyEvent evt) =>
        await _db.Set<WarrantyEvent>().AddAsync(evt);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
