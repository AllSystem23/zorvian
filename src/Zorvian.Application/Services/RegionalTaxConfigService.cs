using Zorvian.Application.DTOs.Tax;
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

    public async Task<IEnumerable<RegionalTaxConfigResponse>> GetAllAsync(Guid companyId)
    {
        var items = await _repository.GetAllAsync(companyId);
        return items.Select(MapToResponse);
    }

    public async Task<RegionalTaxConfigResponse?> GetByIdAsync(Guid id, Guid companyId)
    {
        var item = await _repository.GetByIdAsync(id, companyId);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<IEnumerable<RegionalTaxConfigResponse>> GetActiveTaxesAsync(string countryCode, Guid companyId)
    {
        var items = await _repository.GetActiveTaxesAsync(countryCode, companyId);
        return items.Select(MapToResponse);
    }

    public async Task<RegionalTaxConfigResponse> CreateAsync(CreateRegionalTaxConfigRequest request, Guid companyId)
    {
        var existing = await _repository.FindByCompositeKeyAsync(
            request.CountryCode, request.TaxType, companyId);

        if (existing is not null)
        {
            existing.Rate = request.Rate;
            existing.EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow;
            existing.IsActive = true;
            await _repository.SaveChangesAsync();
            return MapToResponse(existing);
        }

        var entity = new RegionalTaxConfiguration
        {
            CompanyId = companyId,
            CountryCode = request.CountryCode,
            TaxType = request.TaxType,
            Rate = request.Rate,
            EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow,
            IsActive = true
        };

        await _repository.AddAsync(entity);
        return MapToResponse(entity);
    }

    public async Task<RegionalTaxConfigResponse?> UpdateAsync(Guid id, UpdateRegionalTaxConfigRequest request, Guid companyId)
    {
        var entity = await _repository.GetByIdAsync(id, companyId);
        if (entity is null) return null;

        if (request.CountryCode is not null) entity.CountryCode = request.CountryCode;
        if (request.TaxType is not null) entity.TaxType = request.TaxType;
        if (request.Rate.HasValue) entity.Rate = request.Rate.Value;
        if (request.EffectiveDate.HasValue) entity.EffectiveDate = request.EffectiveDate.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        await _repository.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid companyId)
    {
        var entity = await _repository.GetByIdAsync(id, companyId);
        if (entity is null) return false;

        await _repository.DeleteAsync(entity);
        return true;
    }

    private static RegionalTaxConfigResponse MapToResponse(RegionalTaxConfiguration entity) => new(
        entity.Id,
        entity.CountryCode,
        entity.TaxType,
        entity.Rate,
        entity.EffectiveDate,
        entity.IsActive
    );
}
