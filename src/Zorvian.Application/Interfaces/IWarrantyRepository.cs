using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyRepository
{
    Task<Warranty?> GetByIdAsync(Guid id);
    Task<List<Warranty>> GetFilteredAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? status, bool? expiringSoon, Guid branchId);
    Task<string> GenerateWarrantyNumberAsync(Guid companyId);
    Task AddAsync(Warranty warranty);
    Task UpdateAsync(Warranty warranty);
    Task SaveChangesAsync();
}
