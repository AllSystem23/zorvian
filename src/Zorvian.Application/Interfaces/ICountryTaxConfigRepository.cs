using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICountryTaxConfigRepository
{
    Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode);
    Task<List<CountryTaxConfig>> GetAllAsync();
    Task<CountryTaxConfig?> GetByIdAsync(Guid id);
    Task AddAsync(CountryTaxConfig entity);
    Task UpdateAsync(CountryTaxConfig entity);
    Task DeleteAsync(CountryTaxConfig entity);
    Task<bool> ExistsByCountryCodeAsync(string countryCode, Guid? excludeId = null);
    Task<int> SaveChangesAsync();
}
