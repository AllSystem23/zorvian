using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICountryTaxConfigRepository
{
    Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode);
}
