using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAssetMaintenanceRepository
{
    Task<AssetMaintenance?> GetByIdAsync(Guid id);
    Task<List<AssetMaintenance>> GetByAssetIdAsync(Guid assetId);
    Task<List<AssetMaintenance>> GetUpcomingAsync(Guid companyId, int days);
    Task AddAsync(AssetMaintenance maintenance);
    Task UpdateAsync(AssetMaintenance maintenance);
    Task SaveChangesAsync();
}
