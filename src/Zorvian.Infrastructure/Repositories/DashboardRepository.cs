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
    /// Ultra-optimized: returns ALL scalar KPI values in a single raw SQL round-trip.
    /// Uses 11 correlated subqueries — the DB executes them in one network call.
    /// </summary>
    public async Task<DashboardKpiScalars> GetAllKpiScalarsRawAsync(
        string tenantId, bool isSuperAdmin, DateOnly thirtyDaysAgo, int currentMonth, int lastMonth)
    {
        var sql = @"
            SELECT
                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                ) AS ""TotalEmployees"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""Status"" = 'active'
                ) AS ""ActiveEmployees"",

                (SELECT COUNT(*) FROM ""VacationRequests"" v
                 WHERE (v.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND v.""IsDeleted"" = false
                   AND v.""Status"" = 'pending'
                ) AS ""PendingVacationRequests"",

                (SELECT COUNT(*) FROM ""PermissionRequests"" p
                 WHERE (p.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND p.""IsDeleted"" = false
                   AND p.""Status"" = 'pending'
                ) AS ""PendingPermissionRequests"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""Status"" = 'active' AND EXTRACT(MONTH FROM e.""DateOfBirth"") = @currentMonth
                ) AS ""BirthdayCount"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""Status"" = 'active' AND EXTRACT(MONTH FROM e.""HireDate"") = @currentMonth
                ) AS ""AnniversaryCount"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""Status"" = 'active' AND EXTRACT(MONTH FROM e.""DateOfBirth"") = @lastMonth
                ) AS ""PrevBirthdayCount"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""Status"" = 'active' AND EXTRACT(MONTH FROM e.""HireDate"") = @lastMonth
                ) AS ""PrevAnniversaryCount"",

                (SELECT COUNT(*) FROM ""VacationRequests"" v
                 WHERE (v.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND v.""IsDeleted"" = false
                   AND (v.""Status"" = 'approved' OR v.""Status"" = 'rejected')
                   AND v.""UpdatedAt"" IS NOT NULL AND v.""UpdatedAt""::date >= @sinceDate
                ) AS ""ResolvedVacationCount"",

                (SELECT COUNT(*) FROM ""PermissionRequests"" p
                 WHERE (p.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND p.""IsDeleted"" = false
                   AND (p.""Status"" = 'approved' OR p.""Status"" = 'rejected')
                   AND p.""UpdatedAt"" IS NOT NULL AND p.""UpdatedAt""::date >= @sinceDate
                ) AS ""ResolvedPermissionCount"",

                (SELECT COUNT(*) FROM ""Employees"" e
                 WHERE (e.""TenantId"" = @tenantId OR @isSuperAdmin = true) AND e.""IsDeleted"" = false
                   AND e.""HireDate"" <= @thirtyDaysAgo
                   AND (e.""TerminationDate"" IS NULL OR e.""TerminationDate"" > @thirtyDaysAgo)
                ) AS ""PreviousMonthActiveEmployees""
        ";

        var result = await _db.Database
            .SqlQueryRaw<DashboardKpiScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin),
                new Npgsql.NpgsqlParameter("@sinceDate", thirtyDaysAgo),
                new Npgsql.NpgsqlParameter("@currentMonth", currentMonth),
                new Npgsql.NpgsqlParameter("@lastMonth", lastMonth),
                new Npgsql.NpgsqlParameter("@thirtyDaysAgo", thirtyDaysAgo))
            .FirstOrDefaultAsync();

        return result ?? new DashboardKpiScalars();
    }

    public async Task<ExecutiveKpiScalars> GetExecutiveKpiScalarsRawAsync(string tenantId, bool isSuperAdmin)
    {
        var sql = @"
            SELECT
                (SELECT COALESCE(SUM(""Total""), 0) FROM ""Sales""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""SaleDate"" >= CURRENT_DATE
                ) AS ""TodaySales"",

                (SELECT COALESCE(SUM(""Total""), 0) FROM ""Sales""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""SaleDate"" >= DATE_TRUNC('month', CURRENT_DATE)
                ) AS ""MonthSales"",

                (SELECT COALESCE(AVG(""Total""), 0) FROM ""Sales""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""SaleDate"" >= DATE_TRUNC('month', CURRENT_DATE)
                ) AS ""AverageTicket"",

                (SELECT COUNT(*) FROM ""Sales""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""SaleDate"" >= CURRENT_DATE
                ) AS ""TodaySalesCount"",

                (SELECT COUNT(*) FROM ""Credits""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Status"" = 'active'
                ) AS ""ActiveCredits"",

                (SELECT COUNT(*) FROM ""Credits""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Status"" = 'overdue'
                ) AS ""OverdueCredits"",

                (SELECT COALESCE(SUM(""Amount""), 0) FROM ""CreditPayments""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""PaymentDate"" >= DATE_TRUNC('month', CURRENT_DATE)
                ) AS ""MonthlyRecovery"",

                (SELECT COALESCE(SUM(""RemainingAmount""), 0) FROM ""Credits""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Status"" != 'paid'
                ) AS ""TotalPortfolio"",

                (SELECT COUNT(*) FROM ""Products""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Stock"" <= 0
                ) AS ""OutOfStockCount"",

                (SELECT COUNT(*) FROM ""Products""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Stock"" > 0 AND ""Stock"" <= ""MinStock""
                ) AS ""LowStockCount"",

                (SELECT COUNT(*) FROM ""Products""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                ) AS ""TotalProducts"",

                (SELECT COALESCE(SUM(""Amount""), 0) FROM ""CashMovements""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Type"" = 'income' AND ""MovementDate"" >= CURRENT_DATE
                ) AS ""TodayIncome"",

                (SELECT COALESCE(SUM(""Amount""), 0) FROM ""CashMovements""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Type"" = 'expense' AND ""MovementDate"" >= CURRENT_DATE
                ) AS ""TodayExpense"",

                (SELECT COUNT(*) FROM ""CashRegisters""
                 WHERE (""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false
                   AND ""Status"" = 'open'
                ) AS ""OpenRegisters"",

                (SELECT COUNT(*) FROM ""Employees""
                 WHERE ((""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false)
                   AND ""Status"" = 'active'
                ) AS ""ActiveEmployees"",

                (SELECT COUNT(*) FROM ""Employees""
                 WHERE ((""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false)
                ) AS ""TotalEmployees"",

                (SELECT COUNT(*) FROM ""VacationRequests""
                 WHERE ((""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false)
                   AND ""Status"" = 'pending'
                ) AS ""PendingVacations"",

                (SELECT COUNT(*) FROM ""PermissionRequests""
                 WHERE ((""TenantId"" = @tenantId OR @isSuperAdmin = true) AND ""IsDeleted"" = false)
                   AND ""Status"" = 'pending'
                ) AS ""PendingPermissions""
        ";

        var result = await _db.Database
            .SqlQueryRaw<ExecutiveKpiScalars>(sql,
                new Npgsql.NpgsqlParameter("@tenantId", tenantId),
                new Npgsql.NpgsqlParameter("@isSuperAdmin", isSuperAdmin))
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

    public async Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end)
    {
        var query = NeedsBypass
            ? _db.Set<VacationRequest>().IgnoreQueryFilters().Where(v => !v.IsDeleted)
            : _db.Set<VacationRequest>().AsQueryable();

        return await query
            .Include(v => v.Employee)
            .Where(v => v.StartDate <= end && v.EndDate >= start
                && (v.Status == "approved" || v.Status == "pending"))
            .ToListAsync();
    }

    public async Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count)
    {
        var query = NeedsBypass
            ? _db.Set<PermissionRequest>().IgnoreQueryFilters().Where(p => !p.IsDeleted)
            : _db.Set<PermissionRequest>().AsQueryable();

        return await query
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<VacationRequest>> GetRecentVacationsAsync(int count)
    {
        var query = NeedsBypass
            ? _db.VacationRequests.IgnoreQueryFilters().Where(v => !v.IsDeleted)
            : _db.VacationRequests.AsQueryable();

        return await query
            .Include(v => v.Employee)
            .OrderByDescending(v => v.CreatedAt)
            .Take(count)
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
