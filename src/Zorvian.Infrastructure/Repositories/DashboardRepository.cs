using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly ZorvianDbContext _db;

    public DashboardRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<int> GetTotalEmployeesAsync()
    {
        return await _db.Employees.CountAsync();
    }

    public async Task<int> GetActiveEmployeesAsync()
    {
        return await _db.Employees.CountAsync(e => e.Status == "active");
    }

    public async Task<int> GetInactiveEmployeesAsync()
    {
        return await _db.Employees.CountAsync(e => e.Status != "active");
    }

    public async Task<List<(string Name, int Count)>> GetEmployeesByDepartmentAsync()
    {
        var data = await _db.Departments
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
        return await _db.Set<VacationRequest>()
            .CountAsync(v => v.Status == "pending");
    }

    public async Task<int> GetPendingPermissionRequestsAsync()
    {
        return await _db.Set<PermissionRequest>()
            .CountAsync(p => p.Status == "pending");
    }

    public async Task<List<Employee>> GetEmployeesWithBirthdayThisMonthAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.Employees
            .Where(e => e.DateOfBirth!.Value.Month == now.Month && e.Status == "active")
            .ToListAsync();
    }

    public async Task<List<Employee>> GetEmployeesWithAnniversaryThisMonthAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.Employees
            .Where(e => e.HireDate.Month == now.Month && e.Status == "active")
            .ToListAsync();
    }

    public async Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end)
    {
        return await _db.Set<VacationRequest>()
            .Include(v => v.Employee)
            .Where(v => v.StartDate <= end && v.EndDate >= start
                && (v.Status == "approved" || v.Status == "pending"))
            .ToListAsync();
    }

    public async Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count)
    {
        return await _db.Set<PermissionRequest>()
            .Include(p => p.Employee)
            .Include(p => p.LeaveType)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<VacationRequest>> GetRecentVacationsAsync(int count)
    {
        return await _db.VacationRequests
            .Include(v => v.Employee)
            .OrderByDescending(v => v.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<(string Department, decimal Amount)>> GetPayrollCostByDepartmentAsync()
    {
        var latestRun = await _db.PayrollRuns
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (latestRun == null) return new List<(string, decimal)>();

        return await _db.PayrollDetails
            .Where(d => d.PayrollRunId == latestRun.Id)
            .GroupBy(d => d.Employee!.Department!.Name)
            .Select(g => new ValueTuple<string, decimal>(g.Key, g.Sum(d => d.GrossPay)))
            .ToListAsync();
    }

    public async Task<List<(string Period, decimal Amount)>> GetPayrollHistoryAsync(int count)
    {
        return await _db.PayrollRuns
            .Include(r => r.PayrollPeriod)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .Select(r => new ValueTuple<string, decimal>(r.PayrollPeriod!.Name, r.TotalNetPay))
            .ToListAsync();
    }
}
