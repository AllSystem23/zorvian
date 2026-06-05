using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAssetDisposalRepository
{
    Task<AssetDisposal?> GetByAssetIdAsync(Guid assetId);
    Task AddAsync(AssetDisposal disposal);
    Task SaveChangesAsync();
}
