using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AssetRevaluationRepository : IAssetRevaluationRepository
{
    private readonly ZorvianDbContext _db;

    public AssetRevaluationRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<AssetRevaluation>> GetByAssetIdAsync(Guid assetId) =>
        await _db.Set<AssetRevaluation>()
            .Where(r => r.FixedAssetId == assetId)
            .OrderByDescending(r => r.RevaluationDate)
            .ToListAsync();

    public async Task AddAsync(AssetRevaluation revaluation) => await _db.Set<AssetRevaluation>().AddAsync(revaluation);
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
