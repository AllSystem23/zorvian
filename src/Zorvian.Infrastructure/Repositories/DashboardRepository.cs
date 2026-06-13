using Microsoft.EntityFrameworkCore;
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

    private bool NeedsBypass => _tenant.TenantId.Value == Guid.Empty;

    private IQueryable<T> Query<T>() where T : class
    {
        var set = _db.Set<T>().AsQueryable();
        return NeedsBypass ? set.IgnoreQueryFilters().Where(e => !EF.Property<bool>(e, "IsDeleted")) : set;
    }

    public async Task<int> GetTotalEmployeesAsync()
    {
        return await Query<Employee>().CountAsync();
    }

    public async Task<int> GetActiveEmployeesAsync()
    {
        return await Query<Employee>().CountAsync(e => e.Status == "active");
    }

    public async Task<int> GetPreviousMonthActiveEmployeesAsync()
    {
        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        return await Query<Employee>()
            .CountAsync(e => e.HireDate <= thirtyDaysAgo
                && (e.TerminationDate == null || e.TerminationDate > thirtyDaysAgo));
    }

    public async Task<int> GetInactiveEmployeesAsync()
    {
        return await Query<Employee>().CountAsync(e => e.Status != "active");
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

    public async Task<int> GetPendingVacationRequestsAsync()
    {
        return await Query<VacationRequest>().CountAsync(v => v.Status == "pending");
    }

    public async Task<int> GetPendingPermissionRequestsAsync()
    {
        return await Query<PermissionRequest>().CountAsync(p => p.Status == "pending");
    }

    public async Task<List<Employee>> GetEmployeesWithBirthdayThisMonthAsync()
    {
        var now = DateTime.UtcNow;
        return await Query<Employee>()
            .Where(e => e.DateOfBirth!.Value.Month == now.Month && e.Status == "active")
            .ToListAsync();
    }

    public async Task<List<Employee>> GetEmployeesWithAnniversaryThisMonthAsync()
    {
        var now = DateTime.UtcNow;
        return await Query<Employee>()
            .Where(e => e.HireDate.Month == now.Month && e.Status == "active")
            .ToListAsync();
    }

    public async Task<int> GetResolvedVacationCountSinceAsync(DateOnly since)
    {
        return await Query<VacationRequest>()
            .CountAsync(v => (v.Status == "approved" || v.Status == "rejected")
                && v.UpdatedAt != null && DateOnly.FromDateTime(v.UpdatedAt.Value) >= since);
    }

    public async Task<int> GetResolvedPermissionCountSinceAsync(DateOnly since)
    {
        return await Query<PermissionRequest>()
            .CountAsync(p => (p.Status == "approved" || p.Status == "rejected")
                && p.UpdatedAt != null && DateOnly.FromDateTime(p.UpdatedAt.Value) >= since);
    }

    public async Task<List<Employee>> GetEmployeesWithBirthdayInMonthAsync(int month)
    {
        return await Query<Employee>()
            .Where(e => e.DateOfBirth!.Value.Month == month && e.Status == "active")
            .ToListAsync();
    }

    public async Task<List<Employee>> GetEmployeesWithAnniversaryInMonthAsync(int month)
    {
        return await Query<Employee>()
            .Where(e => e.HireDate.Month == month && e.Status == "active")
            .ToListAsync();
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
