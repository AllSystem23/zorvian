using Microsoft.EntityFrameworkCore;
using Zorvian.Application.DTOs.Dashboard;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ZorvianDbContext _db;
    private readonly Zorvian.Core.Interfaces.ITenantContext _tenant;

    public DashboardRepository(ZorvianDbContext db, Zorvian.Core.Interfaces.ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private bool NeedsBypass => _tenant.TenantId.Value == Guid.Empty || _tenant.IsSuperAdmin;

    private IQueryable<T> Query<T>() where T : class
    {
        var set = _db.Set<T>().AsQueryable();
        return NeedsBypass ? set.IgnoreQueryFilters().Where(e => !EF.Property<bool>(e, "IsDeleted")) : set;
    }

    /// <summary>
    /// Optimized: one raw SQL round-trip for all scalar HR KPIs.
    /// CTEs materialize tenant-filtered rows once; FILTER/COUNT aggregates avoid repeated table scans.
    /// </summary>
    public async Task<DashboardKpiScalars> GetAllKpiScalarsRawAsync(
        Guid? companyId, bool isSuperAdmin, DateOnly thirtyDaysAgo, int currentMonth, int lastMonth)
    {
        var sql = @"
            WITH employees AS (
                SELECT ""Status"", ""DateOfBirth"", ""HireDate"", ""TerminationDate""
                FROM ""Employees""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId::text) AND ""IsDeleted"" = false
            ),
            vacations AS (
                SELECT ""Status"", ""UpdatedAt""
                FROM ""VacationRequests""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId::text) AND ""IsDeleted"" = false
            ),
            permissions AS (
                SELECT ""Status"", ""UpdatedAt""
                FROM ""PermissionRequests""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId::text) AND ""IsDeleted"" = false
            )
            SELECT
                (SELECT COUNT(*) FROM employees)::int AS ""TotalEmployees"",
                (SELECT COUNT(*) FROM employees WHERE ""Status"" = 'active')::int AS ""ActiveEmployees"",
                (SELECT COUNT(*) FROM vacations WHERE ""Status"" = 'pending')::int AS ""PendingVacationRequests"",
                (SELECT COUNT(*) FROM permissions WHERE ""Status"" = 'pending')::int AS ""PendingPermissionRequests"",
                (SELECT COUNT(*) FROM employees WHERE ""Status"" = 'active' AND EXTRACT(MONTH FROM ""DateOfBirth"") = @currentMonth)::int AS ""BirthdayCount"",
                (SELECT COUNT(*) FROM employees WHERE ""Status"" = 'active' AND EXTRACT(MONTH FROM ""HireDate"") = @currentMonth)::int AS ""AnniversaryCount"",
                (SELECT COUNT(*) FROM employees WHERE ""Status"" = 'active' AND EXTRACT(MONTH FROM ""DateOfBirth"") = @lastMonth)::int AS ""PrevBirthdayCount"",
                (SELECT COUNT(*) FROM employees WHERE ""Status"" = 'active' AND EXTRACT(MONTH FROM ""HireDate"") = @lastMonth)::int AS ""PrevAnniversaryCount"",
                (SELECT COUNT(*) FROM vacations WHERE (""Status"" = 'approved' OR ""Status"" = 'rejected') AND ""UpdatedAt"" >= @sinceDate)::int AS ""ResolvedVacationCount"",
                (SELECT COUNT(*) FROM permissions WHERE (""Status"" = 'approved' OR ""Status"" = 'rejected') AND ""UpdatedAt"" >= @sinceDate)::int AS ""ResolvedPermissionCount"",
                (SELECT COUNT(*) FROM employees WHERE ""HireDate"" <= @thirtyDaysAgo AND (""TerminationDate"" IS NULL OR ""TerminationDate"" > @thirtyDaysAgo))::int AS ""PreviousMonthActiveEmployees""
        ";

        var result = await _db.Database
            .SqlQueryRaw<DashboardKpiScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", (object?)companyId ?? DBNull.Value),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@sinceDate", thirtyDaysAgo),
                new Npgsql.NpgsqlParameter("@currentMonth", currentMonth),
                new Npgsql.NpgsqlParameter("@lastMonth", lastMonth),
                new Npgsql.NpgsqlParameter("@thirtyDaysAgo", thirtyDaysAgo))
            .FirstOrDefaultAsync();

        return result ?? new DashboardKpiScalars();
    }

    public async Task<ExecutiveKpiScalars> GetExecutiveKpiScalarsRawAsync(Guid? companyId, bool isSuperAdmin)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var tenantIdValue = companyId?.ToString() ?? string.Empty;
        var sql = @"
            WITH sales AS (
                SELECT
                    SUM(""Total"") FILTER (WHERE ""SaleDate"" >= @today::date) AS ""TodaySales"",
                    SUM(""Total"") AS ""MonthSales"",
                    AVG(""Total"") AS ""AverageTicket"",
                    COUNT(*) FILTER (WHERE ""SaleDate"" >= @today::date) AS ""TodaySalesCount""
                FROM ""Sales""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
                  AND ""SaleDate"" >= @monthStart::date
            ),
            credits AS (
                SELECT
                    COUNT(*) FILTER (WHERE ""Status"" = 'active') AS ""ActiveCredits"",
                    COUNT(*) FILTER (WHERE ""Status"" = 'overdue') AS ""OverdueCredits"",
                    COALESCE(SUM(""Balance"") FILTER (WHERE ""Status"" <> 'paid'), 0) AS ""TotalPortfolio""
                FROM ""Credits""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            ),
            payments AS (
                SELECT COALESCE(SUM(""Amount""), 0) AS ""MonthlyRecovery""
                FROM ""CreditPayments""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
                  AND ""PaymentDate"" >= @monthStart::date
            ),
            products AS (
                SELECT
                    COUNT(*) FILTER (WHERE ""Stock"" <= 0) AS ""OutOfStockCount"",
                    COUNT(*) FILTER (WHERE ""Stock"" > 0 AND ""Stock"" <= ""MinStock"") AS ""LowStockCount"",
                    COUNT(*) AS ""TotalProducts""
                FROM ""Products""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            ),
            cash AS (
                SELECT
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""Type"" = 'income'), 0) AS ""TodayIncome"",
                    COALESCE(SUM(""Amount"") FILTER (WHERE ""Type"" = 'expense'), 0) AS ""TodayExpense""
                FROM ""CashMovements""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
                  AND ""MovementDate"" >= @today::date
            ),
            registers AS (
                SELECT COUNT(*) FILTER (WHERE ""Status"" = 'open') AS ""OpenRegisters""
                FROM ""CashRegisters""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            ),
            employees AS (
                SELECT
                    COUNT(*) FILTER (WHERE ""Status"" = 'active') AS ""ActiveEmployees"",
                    COUNT(*) AS ""TotalEmployees""
                FROM ""Employees""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            ),
            vacations AS (
                SELECT COUNT(*) FILTER (WHERE ""Status"" = 'pending') AS ""PendingVacations""
                FROM ""VacationRequests""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            ),
            permissions AS (
                SELECT COUNT(*) FILTER (WHERE ""Status"" = 'pending') AS ""PendingPermissions""
                FROM ""PermissionRequests""
                WHERE (@isSuperAdmin = true OR ""TenantId"" = @tenantId) AND ""IsDeleted"" = false
            )
            SELECT
                COALESCE(s.""TodaySales"", 0)::decimal AS ""TodaySales"",
                COALESCE(s.""MonthSales"", 0)::decimal AS ""MonthSales"",
                COALESCE(s.""AverageTicket"", 0)::decimal AS ""AverageTicket"",
                COALESCE(s.""TodaySalesCount"", 0)::int AS ""TodaySalesCount"",
                COALESCE(c.""ActiveCredits"", 0)::int AS ""ActiveCredits"",
                COALESCE(c.""OverdueCredits"", 0)::int AS ""OverdueCredits"",
                COALESCE(p.""MonthlyRecovery"", 0)::decimal AS ""MonthlyRecovery"",
                COALESCE(c.""TotalPortfolio"", 0)::decimal AS ""TotalPortfolio"",
                COALESCE(pr.""OutOfStockCount"", 0)::int AS ""OutOfStockCount"",
                COALESCE(pr.""LowStockCount"", 0)::int AS ""LowStockCount"",
                COALESCE(pr.""TotalProducts"", 0)::int AS ""TotalProducts"",
                COALESCE(ca.""TodayIncome"", 0)::decimal AS ""TodayIncome"",
                COALESCE(ca.""TodayExpense"", 0)::decimal AS ""TodayExpense"",
                COALESCE(r.""OpenRegisters"", 0)::int AS ""OpenRegisters"",
                COALESCE(e.""ActiveEmployees"", 0)::int AS ""ActiveEmployees"",
                COALESCE(e.""TotalEmployees"", 0)::int AS ""TotalEmployees"",
                COALESCE(v.""PendingVacations"", 0)::int AS ""PendingVacations"",
                COALESCE(pm.""PendingPermissions"", 0)::int AS ""PendingPermissions""
            FROM sales s
            CROSS JOIN credits c
            CROSS JOIN payments p
            CROSS JOIN products pr
            CROSS JOIN cash ca
            CROSS JOIN registers r
            CROSS JOIN employees e
            CROSS JOIN vacations v
            CROSS JOIN permissions pm
        ";

        var result = await _db.Database
            .SqlQueryRaw<ExecutiveKpiScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantIdValue),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@today", today),
                new Npgsql.NpgsqlParameter("@monthStart", monthStart))
            .FirstOrDefaultAsync();

        return result ?? new ExecutiveKpiScalars();
    }

    public async Task<List<(string Name, int Count)>> GetEmployeesByDepartmentAsync()
    {
        var data = await Query<Department>()
            .Select(d => new
            {
                d.Name,
                Count = d.Employees.Count(e => e.Status == "active")
            })
            .ToListAsync();

        return data.Select(d => (d.Name, d.Count)).ToList();
    }

    public async Task<int> GetTotalEmployeesAsync()
    {
        return await Query<Employee>().CountAsync();
    }

    public async Task<int> GetActiveEmployeesAsync()
    {
        return await Query<Employee>().CountAsync(e => e.Status == "active");
    }

    public async Task<int> GetPendingVacationRequestsAsync()
    {
        return await Query<VacationRequest>().CountAsync(v => v.Status == "pending");
    }

    public async Task<int> GetPendingPermissionRequestsAsync()
    {
        return await Query<PermissionRequest>().CountAsync(p => p.Status == "pending");
    }

    public async Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end, int page, int pageSize)
    {
        var query = NeedsBypass
            ? _db.Set<VacationRequest>().IgnoreQueryFilters().Where(v => !v.IsDeleted)
            : _db.Set<VacationRequest>().AsQueryable();

        return await query
            .Include(v => v.Employee)
            .Where(v => v.StartDate <= end && v.EndDate >= start
                && (v.Status == "approved" || v.Status == "pending"))
            .OrderByDescending(v => v.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count, int page, int pageSize)
    {
        var query = NeedsBypass
            ? _db.Set<PermissionRequest>().IgnoreQueryFilters().Where(p => !p.IsDeleted)
            : _db.Set<PermissionRequest>().AsQueryable();

        return await query
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<VacationRequest>> GetRecentVacationsAsync(int count, int page, int pageSize)
    {
        var query = NeedsBypass
            ? _db.VacationRequests.IgnoreQueryFilters().Where(v => !v.IsDeleted)
            : _db.VacationRequests.AsQueryable();

        return await query
            .Include(v => v.Employee)
            .OrderByDescending(v => v.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<(string Department, decimal Amount)>> GetPayrollCostByDepartmentAsync()
    {
        var latestRun = NeedsBypass
            ? await _db.PayrollRuns.IgnoreQueryFilters().Where(r => !r.IsDeleted).OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync()
            : await _db.PayrollRuns.OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync();

        if (latestRun == null) return new List<(string, decimal)>();

        var detailsQuery = NeedsBypass
            ? _db.PayrollDetails.IgnoreQueryFilters().Where(d => !d.IsDeleted)
            : _db.PayrollDetails.AsQueryable();

        return await detailsQuery
            .Where(d => d.PayrollRunId == latestRun.Id)
            .GroupBy(d => d.Employee!.Department!.Name)
            .Select(g => new ValueTuple<string, decimal>(g.Key, g.Sum(d => d.GrossPay)))
            .ToListAsync();
    }

    public async Task<List<(string Period, decimal Amount)>> GetPayrollHistoryAsync(int count)
    {
        var query = NeedsBypass
            ? _db.PayrollRuns.IgnoreQueryFilters().Where(r => !r.IsDeleted)
            : _db.PayrollRuns.AsQueryable();

        return await query
            .Include(r => r.PayrollPeriod)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .Select(r => new ValueTuple<string, decimal>(r.PayrollPeriod!.Name, r.TotalNetPay))
            .ToListAsync();
    }
}
