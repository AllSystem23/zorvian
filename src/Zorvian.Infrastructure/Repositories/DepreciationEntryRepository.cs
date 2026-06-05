using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DepreciationEntryRepository : IDepreciationEntryRepository
{
    private readonly ZorvianDbContext _db;

    public DepreciationEntryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<DepreciationEntry>> GetByAssetIdAsync(Guid assetId) =>
        await _db.Set<DepreciationEntry>()
            .Where(e => e.FixedAssetId == assetId)
            .OrderBy(e => e.PeriodDate)
            .ToListAsync();

    public async Task<List<DepreciationEntry>> GetByAssetIdOrderedAsync(Guid assetId) =>
        await _db.Set<DepreciationEntry>()
            .Where(e => e.FixedAssetId == assetId)
            .OrderByDescending(e => e.PeriodDate)
            .ToListAsync();

    public async Task<DepreciationEntry?> GetLastByAssetIdAsync(Guid assetId) =>
        await _db.Set<DepreciationEntry>()
            .Where(e => e.FixedAssetId == assetId)
            .OrderByDescending(e => e.PeriodDate)
            .FirstOrDefaultAsync();

    public async Task AddAsync(DepreciationEntry entry) => await _db.Set<DepreciationEntry>().AddAsync(entry);
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
