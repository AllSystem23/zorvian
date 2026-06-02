using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface ISupplierRepository
{
    Task<List<Supplier>> GetAllAsync(Guid companyId);
    Task<Supplier?> GetByIdAsync(Guid id);
    Task AddAsync(Supplier supplier);
    Task UpdateAsync(Supplier supplier);
    Task DeleteAsync(Supplier supplier);
    Task SaveChangesAsync();
}
