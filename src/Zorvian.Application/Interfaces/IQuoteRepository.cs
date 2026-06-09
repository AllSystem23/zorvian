using Zorvian.Core.Entities;
using Zorvian.Core.Enums;

namespace Zorvian.Application.Interfaces;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id);
    Task<List<Quote>> GetFilteredAsync(Guid? clientId, QuoteStatus? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, QuoteStatus? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId);
    Task<string> GenerateNumberAsync(Guid companyId);
    Task AddAsync(Quote quote);
    Task UpdateAsync(Quote quote);
    Task UpdateStatusAsync(Guid id, QuoteStatus status);
    Task DeleteAsync(Quote quote);
    Task SaveChangesAsync();
}
