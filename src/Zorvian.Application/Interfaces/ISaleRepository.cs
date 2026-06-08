using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISaleRepository
{
    Task<Sale?> GetByIdAsync(Guid id);
    Task<List<Sale>> GetFilteredAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId);
    Task<string> GenerateInvoiceNumberAsync(Guid companyId);
    Task<decimal> GetTodaySalesAsync(Guid branchId);
    Task<decimal> GetMonthSalesAsync(Guid branchId);
    Task<decimal> GetAverageTicketAsync(Guid branchId);
    Task<int> GetTodaySalesCountAsync(Guid branchId);
    Task AddAsync(Sale sale);
    Task UpdateAsync(Sale sale);
    Task SaveChangesAsync();
}
