using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IFixedAssetRepository
{
    Task<FixedAsset?> GetByIdAsync(Guid id);
    Task<FixedAsset?> GetByCodeAsync(string code, Guid companyId);
    Task<List<FixedAsset>> GetFilteredAsync(Guid? categoryId, string? status, Guid? locationId, Guid? departmentId,
        string? search, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize, Guid? branchId = null);
    Task<int> GetFilteredCountAsync(Guid? categoryId, string? status, Guid? locationId, Guid? departmentId,
        string? search, DateTime? fromDate, DateTime? toDate, Guid companyId, Guid? branchId = null);
    Task<List<FixedAsset>> GetActiveForDepreciationAsync(DateTime periodDate, Guid companyId);
    Task<int> GetTotalCountAsync(Guid companyId);
    Task<string> GenerateCodeAsync(Guid companyId);
    Task AddAsync(FixedAsset asset);
    Task UpdateAsync(FixedAsset asset);
    Task DeleteAsync(FixedAsset asset);
    Task SaveChangesAsync();
}
