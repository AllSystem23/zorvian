using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IFixedAssetCategoryRepository
{
    Task<FixedAssetCategory?> GetByIdAsync(Guid id);
    Task<List<FixedAssetCategory>> GetAllAsync(Guid companyId);
    Task AddAsync(FixedAssetCategory category);
    Task UpdateAsync(FixedAssetCategory category);
    Task DeleteAsync(FixedAssetCategory category);
    Task SaveChangesAsync();
}
