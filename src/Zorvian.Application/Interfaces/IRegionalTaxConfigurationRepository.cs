using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IRegionalTaxConfigurationRepository
{
    Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId);
    Task AddOrUpdateAsync(RegionalTaxConfiguration taxConfig);
}
