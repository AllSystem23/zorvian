using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class CountryTaxConfigService : ICountryTaxConfigService
{
    private readonly ICountryTaxConfigRepository _repo;

    public CountryTaxConfigService(ICountryTaxConfigRepository repo) => _repo = repo;

    public async Task<List<CountryTaxConfigResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return items.Select(MapToResponse).ToList();
    }

    public async Task<CountryTaxConfigResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<CountryTaxConfigResponse?> GetByCountryCodeAsync(string countryCode)
    {
        var item = await _repo.GetByCountryCodeAsync(countryCode);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<CountryTaxConfigResponse> CreateAsync(CreateCountryTaxConfigRequest request)
    {
        var exists = await _repo.ExistsByCountryCodeAsync(request.CountryCode);
        if (exists)
            throw new InvalidOperationException($"Ya existe una configuración fiscal para el país '{request.CountryCode}'.");

        var entity = new CountryTaxConfig
        {
            CountryCode = request.CountryCode.ToUpper(),
            CountryName = request.CountryName,
            Currency = request.Currency,
            InssEmployeeRate = request.InssEmployeeRate,
            InssEmployeeMax = request.InssEmployeeMax,
            InssEmployerRate = request.InssEmployerRate,
            InssEmployerMax = request.InssEmployerMax,
            OtherEmployerRate = request.OtherEmployerRate,
            OtherEmployerName = request.OtherEmployerName,
            IrExemptAmount = request.IrExemptAmount,
            IrTableJson = request.IrTableJson,
            VacationDaysPerYear = request.VacationDaysPerYear,
            ChristmasBonusPercentage = request.ChristmasBonusPercentage,
            IndemnityDaysPerYear = request.IndemnityDaysPerYear,
            MaxIndemnityYears = request.MaxIndemnityYears,
            HasThirteenthMonth = request.HasThirteenthMonth,
            HasFourteenthMonth = request.HasFourteenthMonth,
            IsActive = true,
        };

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<CountryTaxConfigResponse?> UpdateAsync(Guid id, UpdateCountryTaxConfigRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        if (request.CountryCode is not null)
        {
            var code = request.CountryCode.ToUpper();
            var exists = await _repo.ExistsByCountryCodeAsync(code, id);
            if (exists)
                throw new InvalidOperationException($"Ya existe otra configuración fiscal para el país '{code}'.");
            entity.CountryCode = code;
        }
        if (request.CountryName is not null) entity.CountryName = request.CountryName;
        if (request.Currency is not null) entity.Currency = request.Currency;
        if (request.InssEmployeeRate.HasValue) entity.InssEmployeeRate = request.InssEmployeeRate.Value;
        if (request.InssEmployeeMax.HasValue) entity.InssEmployeeMax = request.InssEmployeeMax.Value;
        if (request.InssEmployerRate.HasValue) entity.InssEmployerRate = request.InssEmployerRate.Value;
        if (request.InssEmployerMax.HasValue) entity.InssEmployerMax = request.InssEmployerMax.Value;
        if (request.OtherEmployerRate.HasValue) entity.OtherEmployerRate = request.OtherEmployerRate.Value;
        if (request.OtherEmployerName is not null) entity.OtherEmployerName = request.OtherEmployerName;
        if (request.IrExemptAmount.HasValue) entity.IrExemptAmount = request.IrExemptAmount.Value;
        if (request.IrTableJson is not null) entity.IrTableJson = request.IrTableJson;
        if (request.VacationDaysPerYear.HasValue) entity.VacationDaysPerYear = request.VacationDaysPerYear.Value;
        if (request.ChristmasBonusPercentage.HasValue) entity.ChristmasBonusPercentage = request.ChristmasBonusPercentage.Value;
        if (request.IndemnityDaysPerYear.HasValue) entity.IndemnityDaysPerYear = request.IndemnityDaysPerYear.Value;
        if (request.MaxIndemnityYears.HasValue) entity.MaxIndemnityYears = request.MaxIndemnityYears.Value;
        if (request.HasThirteenthMonth.HasValue) entity.HasThirteenthMonth = request.HasThirteenthMonth.Value;
        if (request.HasFourteenthMonth.HasValue) entity.HasFourteenthMonth = request.HasFourteenthMonth.Value;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;

        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }

    private static CountryTaxConfigResponse MapToResponse(CountryTaxConfig e) => new(
        e.Id,
        e.CountryCode,
        e.CountryName,
        e.Currency,
        e.InssEmployeeRate,
        e.InssEmployeeMax,
        e.InssEmployerRate,
        e.InssEmployerMax,
        e.OtherEmployerRate,
        e.OtherEmployerName,
        e.IrExemptAmount,
        e.IrTableJson,
        e.VacationDaysPerYear,
        e.ChristmasBonusPercentage,
        e.IndemnityDaysPerYear,
        e.MaxIndemnityYears,
        e.HasThirteenthMonth,
        e.HasFourteenthMonth,
        e.IsActive
    );
}
