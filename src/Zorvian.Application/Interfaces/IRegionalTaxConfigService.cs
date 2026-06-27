using Zorvian.Application.DTOs.Tax;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IRegionalTaxConfigService
{
    Task<IEnumerable<RegionalTaxConfigResponse>> GetAllAsync(Guid companyId);
    Task<RegionalTaxConfigResponse?> GetByIdAsync(Guid id, Guid companyId);
    Task<IEnumerable<RegionalTaxConfigResponse>> GetActiveTaxesAsync(string countryCode, Guid companyId);
    Task<RegionalTaxConfigResponse> CreateAsync(CreateRegionalTaxConfigRequest request, Guid companyId);
    Task<RegionalTaxConfigResponse?> UpdateAsync(Guid id, UpdateRegionalTaxConfigRequest request, Guid companyId);
    Task<bool> DeleteAsync(Guid id, Guid companyId);
}
