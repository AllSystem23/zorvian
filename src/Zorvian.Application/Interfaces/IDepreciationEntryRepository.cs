using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDepreciationEntryRepository
{
    Task<List<DepreciationEntry>> GetByAssetIdAsync(Guid assetId);
    Task<List<DepreciationEntry>> GetByAssetIdOrderedAsync(Guid assetId);
    Task<DepreciationEntry?> GetLastByAssetIdAsync(Guid assetId);
    Task AddAsync(DepreciationEntry entry);
    Task SaveChangesAsync();
}
