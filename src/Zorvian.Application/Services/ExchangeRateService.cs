using Zorvian.Application.DTOs.MultiCurrency;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public interface IExchangeRateService
{
    Task<List<ExchangeRateResponse>> GetAllAsync();
    Task<ExchangeRateResponse?> GetByIdAsync(Guid id);
    Task<ExchangeRateResponse> CreateAsync(CreateExchangeRateRequest request);
    Task<ExchangeRateResponse?> UpdateAsync(Guid id, UpdateExchangeRateRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, DateTime? date = null);
}

public sealed class ExchangeRateService : IExchangeRateService
{
    private readonly IExchangeRateRepository _repo;

    public ExchangeRateService(IExchangeRateRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<ExchangeRateResponse>> GetAllAsync()
    {
        var rates = await _repo.GetAllAsync();
        return rates.Select(ToResponse).ToList();
    }

    public async Task<ExchangeRateResponse?> GetByIdAsync(Guid id)
    {
        var rate = await _repo.GetByIdAsync(id);
        return rate is null ? null : ToResponse(rate);
    }

    public async Task<ExchangeRateResponse> CreateAsync(CreateExchangeRateRequest request)
    {
        var entity = new ExchangeRate
        {
            FromCurrency = request.FromCurrency.ToUpperInvariant(),
            ToCurrency = request.ToCurrency.ToUpperInvariant(),
            Rate = request.Rate,
            EffectiveDate = DateTime.SpecifyKind(request.EffectiveDate, DateTimeKind.Utc),
        };
        var created = await _repo.AddAsync(entity);
        return ToResponse(created);
    }

    public async Task<ExchangeRateResponse?> UpdateAsync(Guid id, UpdateExchangeRateRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        entity.Rate = request.Rate;
        entity.EffectiveDate = DateTime.SpecifyKind(request.EffectiveDate, DateTimeKind.Utc);
        var updated = await _repo.UpdateAsync(entity);
        return ToResponse(updated!);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repo.DeleteAsync(id);
    }

    public async Task<decimal?> GetRateAsync(string fromCurrency, string toCurrency, DateTime? date = null)
    {
        if (string.Equals(fromCurrency, toCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;
        var utcDate = date is not null
            ? DateTime.SpecifyKind(date.Value, DateTimeKind.Utc)
            : (DateTime?)null;
        var rate = await _repo.GetLatestRateAsync(fromCurrency, toCurrency, utcDate);
        return rate?.Rate;
    }

    private static ExchangeRateResponse ToResponse(ExchangeRate x) => new(
        x.Id, x.FromCurrency, x.ToCurrency, x.Rate, x.EffectiveDate
    );
}
