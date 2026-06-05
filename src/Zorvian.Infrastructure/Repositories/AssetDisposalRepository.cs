using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AssetDisposalRepository : IAssetDisposalRepository
{
    private readonly ZorvianDbContext _db;

    public AssetDisposalRepository(ZorvianDbContext db) => _db = db;

    public async Task<AssetDisposal?> GetByAssetIdAsync(Guid assetId) =>
        await _db.Set<AssetDisposal>()
            .FirstOrDefaultAsync(d => d.FixedAssetId == assetId);

    public async Task AddAsync(AssetDisposal disposal) => await _db.Set<AssetDisposal>().AddAsync(disposal);
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
