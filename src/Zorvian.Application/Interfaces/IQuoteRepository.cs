using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id);
    Task<List<Quote>> GetFilteredAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId);
    Task<string> GenerateNumberAsync(Guid companyId);
    Task AddAsync(Quote quote);
    Task UpdateAsync(Quote quote);
    Task DeleteAsync(Quote quote);
    Task SaveChangesAsync();
}
