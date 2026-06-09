using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class RegionalTaxConfigService : IRegionalTaxConfigService
{
    private readonly IRegionalTaxConfigurationRepository _repository;

    public RegionalTaxConfigService(IRegionalTaxConfigurationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId)
    {
        return await _repository.GetActiveTaxesAsync(countryCode, companyId);
    }

    public async Task CreateOrUpdateTaxAsync(RegionalTaxConfiguration taxConfig)
    {
        await _repository.AddOrUpdateAsync(taxConfig);
    }
}
