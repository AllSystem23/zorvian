using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AssetMaintenanceRepository : IAssetMaintenanceRepository
{
    private readonly ZorvianDbContext _db;

    public AssetMaintenanceRepository(ZorvianDbContext db) => _db = db;

    public async Task<AssetMaintenance?> GetByIdAsync(Guid id) =>
        await _db.Set<AssetMaintenance>().FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<AssetMaintenance>> GetByAssetIdAsync(Guid assetId) =>
        await _db.Set<AssetMaintenance>()
            .Where(m => m.FixedAssetId == assetId)
            .OrderByDescending(m => m.MaintenanceDate)
            .ToListAsync();

    public async Task<List<AssetMaintenance>> GetUpcomingAsync(Guid companyId, int days)
    {
        var until = DateTime.UtcNow.AddDays(days);
        return await _db.Set<AssetMaintenance>()
            .Where(m => m.CompanyId == companyId && m.NextMaintenanceDate != null && m.NextMaintenanceDate <= until)
            .OrderBy(m => m.NextMaintenanceDate)
            .ToListAsync();
    }

    public async Task AddAsync(AssetMaintenance maintenance) => await _db.Set<AssetMaintenance>().AddAsync(maintenance);
    public Task UpdateAsync(AssetMaintenance maintenance) { _db.Set<AssetMaintenance>().Update(maintenance); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
