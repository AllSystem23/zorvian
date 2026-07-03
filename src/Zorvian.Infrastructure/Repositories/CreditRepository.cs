using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CreditRepository : ICreditRepository
{
    private readonly ZorvianDbContext _db;

    public CreditRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Credit?> GetByIdAsync(Guid id) =>
        await _db.Set<Credit>()
            .Include(c => c.Client)
            .Include(c => c.Employee)
            .Include(c => c.Sale)
            .Include(c => c.Installments)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Credit>> GetFilteredAsync(Guid? clientId, string? status, string? search, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Credit>()
            .Include(c => c.Client)
            .Include(c => c.Installments)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(c => c.BranchId == branchId);

        if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(c => c.CreditNumber.ToLower().Contains(q)
                || c.Client.FirstName.ToLower().Contains(q)
                || c.Client.LastName.ToLower().Contains(q));
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? status, string? search, Guid branchId)
    {
        var query = _db.Set<Credit>()
            .Include(c => c.Client)
            .AsQueryable();
        if (branchId != Guid.Empty)
            query = query.Where(c => c.BranchId == branchId);
        if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(c => c.CreditNumber.ToLower().Contains(q)
                || c.Client.FirstName.ToLower().Contains(q)
                || c.Client.LastName.ToLower().Contains(q));
        }
        return await query.CountAsync();
    }

    private static readonly System.Threading.SemaphoreSlim _creditNumberSemaphore = new(1, 1);

    public async Task<string> GenerateCreditNumberAsync(Guid companyId)
    {
        await _creditNumberSemaphore.WaitAsync();
        try
        {
            var count = await _db.Set<Credit>().CountAsync(c => c.CompanyId == companyId);
            return $"CRE-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
        }
        finally
        {
            _creditNumberSemaphore.Release();
        }
    }

    public async Task<int> GetActiveCreditsCountAsync(Guid branchId) =>
        await _db.Set<Credit>().CountAsync(c => c.BranchId == branchId && c.Status == "active");

    public async Task<int> GetOverdueCreditsCountAsync(Guid branchId) =>
        await _db.Set<Credit>().CountAsync(c => c.BranchId == branchId && c.Status == "overdue");

    public async Task<decimal> GetMonthlyRecoveryAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<CreditPayment>()
            .Where(p => p.BranchId == branchId && p.PaymentDate >= startOfMonth)
            .SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetTotalPortfolioAsync(Guid branchId) =>
        await _db.Set<Credit>()
            .Where(c => c.BranchId == branchId && c.Status == "active")
            .SumAsync(c => c.Balance);

    public async Task<OverdueDashboardScalars> GetOverdueDashboardScalarsRawAsync(Guid branchId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var sql = @"
            WITH overdue_inst AS (
                SELECT
                    ci.""Id"",
                    ci.""CreditId"",
                    ci.""DueDate"",
                    ci.""Amount"",
                    ci.""Balance"",
                    (@today - ci.""DueDate"")::int AS ""DaysOverdue""
                FROM ""CreditInstallments"" ci
                JOIN ""Credits"" c ON c.""Id"" = ci.""CreditId""
                WHERE c.""BranchId"" = @branchId
                  AND c.""IsDeleted"" = false
                  AND ci.""IsDeleted"" = false
                  AND ci.""Status"" = 'pending'
                  AND ci.""DueDate"" < @today
            ),
            bucket_stats AS (
                SELECT
                    (COUNT(*) FILTER (WHERE ""DaysOverdue"" BETWEEN 1 AND 30))::int AS ""Bucket1InstallmentCount"",
                    (COUNT(DISTINCT ""CreditId"") FILTER (WHERE ""DaysOverdue"" BETWEEN 1 AND 30))::int AS ""Bucket1CreditCount"",
                    COALESCE(SUM(""Balance"") FILTER (WHERE ""DaysOverdue"" BETWEEN 1 AND 30), 0) AS ""Bucket1TotalBalance"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 1 AND 30), 0) AS ""Bucket1TotalAmount"",
                    (COUNT(*) FILTER (WHERE ""DaysOverdue"" BETWEEN 31 AND 60))::int AS ""Bucket2InstallmentCount"",
                    (COUNT(DISTINCT ""CreditId"") FILTER (WHERE ""DaysOverdue"" BETWEEN 31 AND 60))::int AS ""Bucket2CreditCount"",
                    COALESCE(SUM(""Balance"") FILTER (WHERE ""DaysOverdue"" BETWEEN 31 AND 60), 0) AS ""Bucket2TotalBalance"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 31 AND 60), 0) AS ""Bucket2TotalAmount"",
                    (COUNT(*) FILTER (WHERE ""DaysOverdue"" BETWEEN 61 AND 90))::int AS ""Bucket3InstallmentCount"",
                    (COUNT(DISTINCT ""CreditId"") FILTER (WHERE ""DaysOverdue"" BETWEEN 61 AND 90))::int AS ""Bucket3CreditCount"",
                    COALESCE(SUM(""Balance"") FILTER (WHERE ""DaysOverdue"" BETWEEN 61 AND 90), 0) AS ""Bucket3TotalBalance"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 61 AND 90), 0) AS ""Bucket3TotalAmount"",
                    (COUNT(*) FILTER (WHERE ""DaysOverdue"" > 90))::int AS ""Bucket4InstallmentCount"",
                    (COUNT(DISTINCT ""CreditId"") FILTER (WHERE ""DaysOverdue"" > 90))::int AS ""Bucket4CreditCount"",
                    COALESCE(SUM(""Balance"") FILTER (WHERE ""DaysOverdue"" > 90), 0) AS ""Bucket4TotalBalance"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" > 90), 0) AS ""Bucket4TotalAmount""
                FROM overdue_inst
            )
            SELECT
                (SELECT COUNT(*) FROM ""Credits"" WHERE ""BranchId"" = @branchId AND ""IsDeleted"" = false AND ""Status"" = 'active')::int AS ""TotalActiveCredits"",
                (SELECT COUNT(*) FROM ""Credits"" WHERE ""BranchId"" = @branchId AND ""IsDeleted"" = false AND ""Status"" = 'overdue')::int AS ""TotalOverdueCredits"",
                COALESCE((SELECT SUM(""Balance"") FROM ""Credits"" WHERE ""BranchId"" = @branchId AND ""IsDeleted"" = false AND ""Status"" = 'active'), 0)::decimal AS ""TotalPortfolio"",
                COALESCE((SELECT SUM(""Amount"") FROM ""CreditPayments"" WHERE ""BranchId"" = @branchId AND ""IsDeleted"" = false AND ""PaymentDate"" >= @monthStart), 0)::decimal AS ""MonthlyRecovery"",
                COALESCE((SELECT SUM(""Balance"") FROM overdue_inst), 0)::decimal AS ""TotalOverdueBalance"",
                bs.""Bucket1InstallmentCount"",
                bs.""Bucket1CreditCount"",
                bs.""Bucket1TotalBalance"",
                bs.""Bucket1TotalAmount"",
                bs.""Bucket2InstallmentCount"",
                bs.""Bucket2CreditCount"",
                bs.""Bucket2TotalBalance"",
                bs.""Bucket2TotalAmount"",
                bs.""Bucket3InstallmentCount"",
                bs.""Bucket3CreditCount"",
                bs.""Bucket3TotalBalance"",
                bs.""Bucket3TotalAmount"",
                bs.""Bucket4InstallmentCount"",
                bs.""Bucket4CreditCount"",
                bs.""Bucket4TotalBalance"",
                bs.""Bucket4TotalAmount""
            FROM bucket_stats bs
        ";

        return await _db.Database.SqlQueryRaw<OverdueDashboardScalars>(sql,
                new Npgsql.NpgsqlParameter("@branchId", branchId),
                new Npgsql.NpgsqlParameter("@today", today),
                new Npgsql.NpgsqlParameter("@monthStart", monthStart))
            .FirstOrDefaultAsync() ?? new OverdueDashboardScalars();
    }

    public async Task<decimal> GetArAgingTotalPortfolioRawAsync(Guid? companyId, bool isSuperAdmin, string currency)
    {
        var tenantId = companyId?.ToString() ?? string.Empty;
        var sql = @"
            SELECT
                COALESCE(SUM(
                    CASE
                        WHEN c.""CurrencyCode"" = @currency
                             OR c.""ExchangeRateToReporting"" IS NULL
                             OR c.""ExchangeRateToReporting"" <= 0
                        THEN c.""Balance""
                        ELSE c.""Balance"" * c.""ExchangeRateToReporting""
                    END
                ), 0)::decimal AS ""TotalPortfolio""
            FROM ""Credits"" c
            WHERE (@isSuperAdmin = true OR c.""TenantId"" = @tenantId)
              AND c.""IsDeleted"" = false
              AND c.""Status"" IN ('active', 'overdue')
        ";

        var result = await _db.Database
            .SqlQueryRaw<CreditAgingTotalPortfolio>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@currency", currency))
            .FirstOrDefaultAsync() ?? new CreditAgingTotalPortfolio();

        return result.TotalPortfolio;
    }

    public async Task<List<CreditAgingClientItem>> GetArAgingClientsRawAsync(Guid? companyId, bool isSuperAdmin, string currency)
    {
        var tenantId = companyId?.ToString() ?? string.Empty;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sql = @"
            WITH pending_inst AS (
                SELECT
                    cl.""Id"" AS ""ClientId"",
                    concat_ws(' ', cl.""FirstName"", cl.""LastName"") AS ""ClientName"",
                    CASE
                        WHEN c.""CurrencyCode"" = @currency
                             OR c.""ExchangeRateToReporting"" IS NULL
                             OR c.""ExchangeRateToReporting"" <= 0
                        THEN ci.""Balance""
                        ELSE ci.""Balance"" * c.""ExchangeRateToReporting""
                    END AS ""Amount"",
                    (@today::date - ci.""DueDate"")::int AS ""DaysOverdue""
                FROM ""CreditInstallments"" ci
                JOIN ""Credits"" c ON c.""Id"" = ci.""CreditId""
                JOIN ""Clients"" cl ON cl.""Id"" = c.""ClientId""
                WHERE (@isSuperAdmin = true OR c.""TenantId"" = @tenantId)
                  AND c.""IsDeleted"" = false
                  AND ci.""IsDeleted"" = false
                  AND c.""Status"" IN ('active', 'overdue')
                  AND ci.""Status"" IN ('pending', 'overdue')
            ),
            client_totals AS (
                SELECT
                    ""ClientId"",
                    ""ClientName"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" <= 0), 0)::decimal AS ""Current"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 1 AND 30), 0)::decimal AS ""Days30"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 31 AND 60), 0)::decimal AS ""Days60"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" BETWEEN 61 AND 90), 0)::decimal AS ""Days90"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""DaysOverdue"" > 90), 0)::decimal AS ""Days90Plus"",
                    COALESCE(SUM(""Amount""), 0)::decimal AS ""Balance""
                FROM pending_inst
                GROUP BY ""ClientId"", ""ClientName""
            )
            SELECT
                ""ClientName"",
                ""Balance"",
                ""Current"",
                ""Days30"",
                ""Days60"",
                ""Days90"",
                ""Days90Plus""
            FROM client_totals
            WHERE ""Balance"" > 0
            ORDER BY ""Balance"" DESC
            LIMIT 10
        ";

        return await _db.Database
            .SqlQueryRaw<CreditAgingClientItem>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@today", today),
                new Npgsql.NpgsqlParameter("@currency", currency))
            .ToListAsync();
    }

    public async Task<List<CreditInstallment>> GetOverdueInstallmentsAsync(Guid branchId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.Set<CreditInstallment>()
            .Include(ci => ci.Credit)
            .Where(ci => ci.Credit.BranchId == branchId
                && ci.Status == "pending"
                && ci.DueDate < today)
            .OrderBy(ci => ci.DueDate)
            .ToListAsync();
    }

    public async Task<List<CreditInstallment>> GetCriticalOverdueInstallmentsAsync(Guid branchId, int limit)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.Set<CreditInstallment>()
            .Include(ci => ci.Credit)
            .Where(ci => ci.Credit.BranchId == branchId
                && ci.Status == "pending"
                && ci.DueDate < today)
            .OrderByDescending(ci => today.DayNumber - ci.DueDate.DayNumber)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<CreditInstallment>> GetInstallmentsByCreditIdAsync(Guid creditId) =>
        await _db.Set<CreditInstallment>()
            .Where(ci => ci.CreditId == creditId)
            .OrderBy(ci => ci.InstallmentNumber)
            .ToListAsync();

    public async Task AddAsync(Credit credit) =>
        await _db.Set<Credit>().AddAsync(credit);

    public Task UpdateAsync(Credit credit)
    {
        _db.Set<Credit>().Update(credit);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
