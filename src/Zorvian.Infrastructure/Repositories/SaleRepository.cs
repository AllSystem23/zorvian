using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly ZorvianDbContext _db;

    public SaleRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Sale?> GetByIdAsync(Guid id) =>
        await _db.Set<Sale>()
            .Include(s => s.Client)
            .Include(s => s.Employee)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.Category)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p!.Brand)
            .Include(s => s.Payments)
            .Include(s => s.Credit)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Sale>> GetFilteredAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Sale>()
            .Include(s => s.Client)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(s => s.BranchId == branchId);

        if (clientId.HasValue)
            query = query.Where(s => s.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(saleType))
            query = query.Where(s => s.SaleType == saleType);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);
        if (fromDate.HasValue)
            query = query.Where(s => s.SaleDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(s => s.SaleDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(s => s.InvoiceNumber.ToLower().Contains(q)
                || s.Client.FirstName.ToLower().Contains(q)
                || s.Client.LastName.ToLower().Contains(q));
        }

        return await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId)
    {
        var query = _db.Set<Sale>()
            .Include(s => s.Client)
            .AsQueryable();
        if (branchId != Guid.Empty)
            query = query.Where(s => s.BranchId == branchId);

        if (clientId.HasValue) query = query.Where(s => s.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(saleType)) query = query.Where(s => s.SaleType == saleType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(s => s.Status == status);
        if (fromDate.HasValue) query = query.Where(s => s.SaleDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(s => s.SaleDate <= toDate.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(s => s.InvoiceNumber.ToLower().Contains(q)
                || s.Client.FirstName.ToLower().Contains(q)
                || s.Client.LastName.ToLower().Contains(q));
        }

        return await query.CountAsync();
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid companyId)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var count = await _db.Set<Sale>().CountAsync(s => s.CompanyId == companyId);
            return $"FAC-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
        }

        // Use PostgreSQL sequence for atomic, thread-safe number generation
        var raw = await _db.Database.SqlQueryRaw<int>("SELECT nextval('seq_invoice_number')::int").FirstOrDefaultAsync();
        return $"FAC-{DateTime.UtcNow:yyyyMMdd}-{raw:D4}";
    }

    public async Task<decimal> GetTodaySalesAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= today)
            .SumAsync(s => s.Total);
    }

    public async Task<decimal> GetMonthSalesAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= startOfMonth)
            .SumAsync(s => s.Total);
    }

    public async Task<decimal> GetAverageTicketAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= startOfMonth)
            .AverageAsync(s => (decimal?)s.Total) ?? 0m;
    }

    public async Task<int> GetTodaySalesCountAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<Sale>()
            .Where(s => s.BranchId == branchId && s.SaleDate >= today)
            .CountAsync();
    }

    public async Task<List<SalesTrendMetrics>> GetSalesTrendRawAsync(DateTime from, DateTime to, Guid branchId, string currency)
    {
        var query = _db.Set<Sale>()
            .Where(s => !s.IsDeleted && s.SaleDate >= from && s.SaleDate <= to)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(s => s.BranchId == branchId);

        return await query
            .Select(s => new
            {
                Year = s.SaleDate.Year,
                Month = s.SaleDate.Month,
                Amount = s.CurrencyCode == currency
                    || !s.ExchangeRateToReporting.HasValue
                    || s.ExchangeRateToReporting.Value <= 0
                        ? s.Total
                        : s.Total * s.ExchangeRateToReporting.Value
            })
            .GroupBy(s => new { s.Year, s.Month })
            .OrderBy(s => s.Key.Year)
            .ThenBy(s => s.Key.Month)
            .Select(s => new SalesTrendMetrics(
                s.Key.Year,
                s.Key.Month,
                s.Sum(x => x.Amount),
                s.Count(),
                s.Average(x => x.Amount)))
            .ToListAsync();
    }

    public async Task<ExecutiveSalesMetrics> GetExecutiveSalesMetricsRawAsync(
        DateTime lastMonthStart,
        DateTime monthStart,
        DateTime todayStart,
        DateTime yesterdayStart,
        DateTime weekStart,
        DateTime weekEndExclusive,
        Guid branchId,
        string currency)
    {
        var query = _db.Set<Sale>()
            .Where(s => !s.IsDeleted && s.SaleDate >= lastMonthStart && s.SaleDate < weekEndExclusive)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(s => s.BranchId == branchId);

        var periods = await query
            .Select(s => new
            {
                Period = s.SaleDate >= todayStart
                    ? "today"
                    : s.SaleDate >= yesterdayStart
                        ? "yesterday"
                        : s.SaleDate >= monthStart
                            ? "month"
                            : "lastMonth",
                Amount = s.CurrencyCode == currency
                    || !s.ExchangeRateToReporting.HasValue
                    || s.ExchangeRateToReporting.Value <= 0
                        ? s.Total
                        : s.Total * s.ExchangeRateToReporting.Value
            })
            .GroupBy(s => s.Period)
            .Select(s => new { s.Key, Total = s.Sum(x => x.Amount), Count = s.Count() })
            .ToListAsync();

        var periodMap = periods.ToDictionary(p => p.Key, p => (Total: p.Total, Count: p.Count));
        var today = periodMap.GetValueOrDefault("today");
        var yesterday = periodMap.GetValueOrDefault("yesterday");
        var month = periodMap.GetValueOrDefault("month");
        var lastMonth = periodMap.GetValueOrDefault("lastMonth");

        var weekQuery = _db.Set<Sale>()
            .Where(s => !s.IsDeleted && s.SaleDate >= weekStart && s.SaleDate < weekEndExclusive);

        if (branchId != Guid.Empty)
            weekQuery = weekQuery.Where(s => s.BranchId == branchId);

        var weekly = await weekQuery
            .Select(s => new
            {
                Day = s.SaleDate.Date,
                Amount = s.CurrencyCode == currency
                    || !s.ExchangeRateToReporting.HasValue
                    || s.ExchangeRateToReporting.Value <= 0
                        ? s.Total
                        : s.Total * s.ExchangeRateToReporting.Value
            })
            .GroupBy(s => s.Day)
            .OrderBy(s => s.Key)
            .Select(s => new { s.Key, Total = s.Sum(x => x.Amount) })
            .ToListAsync();

        var weeklyMap = weekly.ToDictionary(w => w.Key, w => w.Total);
        var weeklyTrend = new List<decimal>(7);
        for (var i = 0; i < 7; i++)
        {
            weeklyTrend.Add(weeklyMap.GetValueOrDefault(weekStart.AddDays(i)));
        }

        var salesChange = yesterday.Total > 0
            ? (double)((today.Total - yesterday.Total) / yesterday.Total) * 100
            : 0;
        var monthChange = lastMonth.Total > 0
            ? (double)((month.Total - lastMonth.Total) / lastMonth.Total) * 100
            : 0;

        return new ExecutiveSalesMetrics(
            today.Total,
            yesterday.Total,
            salesChange,
            month.Total,
            monthChange,
            month.Count > 0 ? month.Total / month.Count : 0,
            today.Count,
            weeklyTrend);
    }

    public async Task AddAsync(Sale sale) =>
        await _db.Set<Sale>().AddAsync(sale);

    public Task UpdateAsync(Sale sale)
    {
        _db.Set<Sale>().Update(sale);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();

    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await _db.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
