using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IQuoteRepository
{
    Task<Quote?> GetByIdAsync(Guid id);
    Task<List<Quote>> GetFilteredAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? status, DateTime? fromDate, DateTime? toDate, Guid branchId);
    Task<string> GenerateNumberAsync(Guid companyId);
    Task AddAsync(Quote quote);
    Task UpdateAsync(Quote quote);
    Task DeleteAsync(Quote quote);
    Task SaveChangesAsync();
}
