using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IExchangeRateRepository
{
    Task<List<ExchangeRate>> GetAllAsync();
    Task<ExchangeRate?> GetByIdAsync(Guid id);
    Task<ExchangeRate> AddAsync(ExchangeRate rate);
    Task<ExchangeRate?> UpdateAsync(ExchangeRate rate);
    Task<bool> DeleteAsync(Guid id);
    Task<ExchangeRate?> GetLatestRateAsync(string fromCurrency, string toCurrency, DateTime? date = null);
}
