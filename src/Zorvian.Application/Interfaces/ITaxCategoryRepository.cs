using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ITaxCategoryRepository
{
    Task<TaxCategory?> GetByIdAsync(Guid id);
    Task<List<TaxCategory>> GetByCompanyIdAsync(Guid companyId);
    Task AddAsync(TaxCategory taxCategory);
    Task UpdateAsync(TaxCategory taxCategory);
    Task DeleteAsync(TaxCategory taxCategory);
    Task SaveChangesAsync();
}
