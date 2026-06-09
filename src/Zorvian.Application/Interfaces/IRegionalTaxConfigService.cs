using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IRegionalTaxConfigService
{
    Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId);
    Task CreateOrUpdateTaxAsync(RegionalTaxConfiguration taxConfig);
}
