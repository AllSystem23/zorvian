using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAssetRevaluationRepository
{
    Task<List<AssetRevaluation>> GetByAssetIdAsync(Guid assetId);
    Task AddAsync(AssetRevaluation revaluation);
    Task SaveChangesAsync();
}
