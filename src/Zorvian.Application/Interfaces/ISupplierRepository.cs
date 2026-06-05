using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync(Guid companyId);
    Task<Supplier?> GetByIdAsync(Guid id);
    Task<Supplier?> GetByTaxIdAsync(string taxId, Guid companyId);
    Task<List<Supplier>> GetFilteredAsync(string? search, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, Guid companyId);
    Task<string> GenerateCodeAsync(Guid companyId);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
    Task SaveChangesAsync();
}
