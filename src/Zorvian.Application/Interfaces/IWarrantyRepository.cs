using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyRepository
{
    Task<Warranty?> GetByIdAsync(Guid id);
    Task<Warranty?> GetByWarrantyNumberAsync(string warrantyNumber);
    Task<WarrantyClaim?> GetClaimByIdAsync(Guid claimId);
    Task<List<Warranty>> GetFilteredAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId);
    Task<List<Warranty>> GetWarrantiesWithCostsByPeriodAsync(Guid companyId, DateTime from, DateTime to);
    Task<List<Warranty>> GetAtRiskWarrantiesAsync();
    Task<int> GetCountByStatusAsync(string status);
    Task<int> GetBreachedSlaCountAsync();
    Task<string> GenerateWarrantyNumberAsync(Guid companyId);
    Task AddAsync(Warranty warranty);
    Task UpdateAsync(Warranty warranty);
    Task SaveChangesAsync();
}
