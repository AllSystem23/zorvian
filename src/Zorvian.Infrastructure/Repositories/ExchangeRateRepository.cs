using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ExchangeRateRepository : IExchangeRateRepository
{
    private readonly ZorvianDbContext _db;

    public ExchangeRateRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<ExchangeRate>> GetAllAsync()
    {
        return await _db.ExchangeRates.OrderByDescending(x => x.EffectiveDate).ToListAsync();
    }

    public async Task<ExchangeRate?> GetByIdAsync(Guid id)
    {
        return await _db.ExchangeRates.FindAsync(id);
    }

    public async Task<ExchangeRate> AddAsync(ExchangeRate rate)
    {
        _db.ExchangeRates.Add(rate);
        await _db.SaveChangesAsync();
        return rate;
    }

    public async Task<ExchangeRate?> UpdateAsync(ExchangeRate rate)
    {
        _db.ExchangeRates.Update(rate);
        await _db.SaveChangesAsync();
        return rate;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _db.ExchangeRates.FindAsync(id);
        if (entity is null) return false;
        _db.ExchangeRates.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ExchangeRate?> GetLatestRateAsync(string fromCurrency, string toCurrency, DateTime? date = null)
    {
        var effectiveDate = date ?? DateTime.UtcNow;
        return await _db.ExchangeRates
            .Where(x => x.FromCurrency == fromCurrency.ToUpper()
                     && x.ToCurrency == toCurrency.ToUpper()
                     && x.EffectiveDate <= effectiveDate)
            .OrderByDescending(x => x.EffectiveDate)
            .FirstOrDefaultAsync();
    }
}
