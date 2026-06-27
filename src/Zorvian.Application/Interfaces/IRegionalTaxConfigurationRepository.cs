using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IRegionalTaxConfigurationRepository
{
    Task<IEnumerable<RegionalTaxConfiguration>> GetAllAsync(Guid companyId);
    Task<RegionalTaxConfiguration?> GetByIdAsync(Guid id, Guid companyId);
    Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId);
    Task<RegionalTaxConfiguration?> FindByCompositeKeyAsync(string countryCode, string taxType, Guid companyId);
    Task AddAsync(RegionalTaxConfiguration taxConfig);
    Task SaveChangesAsync();
    Task DeleteAsync(RegionalTaxConfiguration taxConfig);
}
