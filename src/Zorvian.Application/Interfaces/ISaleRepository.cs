using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public sealed record SalesTrendMetrics(
    int Year,
    int Month,
    decimal Total,
    int Count,
    decimal AverageTicket
);

public sealed record ExecutiveSalesMetrics(
    decimal TodaySales,
    decimal YesterdaySales,
    double SalesChangePercent,
    decimal MonthSales,
    double MonthSalesChangePercent,
    decimal AverageTicket,
    int TodaySalesCount,
    List<decimal> WeeklyTrend
);

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
    Task<List<SalesTrendMetrics>> GetSalesTrendRawAsync(DateTime from, DateTime to, Guid branchId, string currency);
    Task<ExecutiveSalesMetrics> GetExecutiveSalesMetricsRawAsync(DateTime lastMonthStart, DateTime monthStart, DateTime todayStart, DateTime yesterdayStart, DateTime weekStart, DateTime weekEndExclusive, Guid branchId, string currency);
    Task AddAsync(Sale sale);
    Task UpdateAsync(Sale sale);
    Task SaveChangesAsync();
}
