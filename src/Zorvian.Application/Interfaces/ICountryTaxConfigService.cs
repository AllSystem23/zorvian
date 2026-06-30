using Zorvian.Application.DTOs.Tax;

namespace Zorvian.Application.Interfaces;

public interface ICountryTaxConfigService
{
    Task<List<CountryTaxConfigResponse>> GetAllAsync();
    Task<CountryTaxConfigResponse?> GetByIdAsync(Guid id);
    Task<CountryTaxConfigResponse?> GetByCountryCodeAsync(string countryCode);
    Task<CountryTaxConfigResponse> CreateAsync(CreateCountryTaxConfigRequest request);
    Task<CountryTaxConfigResponse?> UpdateAsync(Guid id, UpdateCountryTaxConfigRequest request);
    Task<bool> DeleteAsync(Guid id);
}
