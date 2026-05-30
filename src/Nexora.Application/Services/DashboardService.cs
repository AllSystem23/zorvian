using Nexora.Application.DTOs.Dashboard;
using Nexora.Application.Interfaces;

namespace Nexora.Application.Services;

public sealed class DashboardService
{
    private const string PermissionType = "permiso";
    private const string VacationType = "vacacion";
    private readonly IDashboardRepository _repo;

    public DashboardService(IDashboardRepository repo)
    {
        _repo = repo;
    }

    public async Task<DashboardKpisResponse> GetKpisAsync()
    {
        var total = await _repo.GetTotalEmployeesAsync();
        var active = await _repo.GetActiveEmployeesAsync();
        var inactive = await _repo.GetInactiveEmployeesAsync();
        var byDept = await _repo.GetEmployeesByDepartmentAsync();
        var pendingVac = await _repo.GetPendingVacationRequestsAsync();
        var pendingPerm = await _repo.GetPendingPermissionRequestsAsync();
        var birthdays = await _repo.GetEmployeesWithBirthdayThisMonthAsync();
        var anniversaries = await _repo.GetEmployeesWithAnniversaryThisMonthAsync();

        return new DashboardKpisResponse(
            total,
            active,
            inactive,
            byDept.Select(d => new DepartmentCount(d.Name, d.Count)).ToList(),
            pendingVac,
            pendingPerm,
            birthdays.Count,
            anniversaries.Count
        );
    }

    public async Task<List<VacationCalendarEvent>> GetVacationCalendarAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateOnly(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var vacations = await _repo.GetVacationsInRangeAsync(start, end);

        return vacations.Select(v => new VacationCalendarEvent(
            v.EmployeeId,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Employee.EmployeeCode ?? "",
            "vacation",
            v.StartDate,
            v.EndDate,
            v.Status
        )).ToList();
    }

    public async Task<List<RecentRequestItem>> GetRecentRequestsAsync(int count = 10)
    {
        var result = new List<RecentRequestItem>();

        var permissions = await _repo.GetRecentPermissionsAsync(count);
        var vacations = await _repo.GetRecentVacationsAsync(count);

        result.AddRange(permissions.Select(p => new RecentRequestItem(
            p.Id,
            PermissionType,
            $"{p.Employee.FirstName} {p.Employee.LastName}",
            p.Status,
            $"{p.LeaveType?.Name}",
            p.CreatedAt
        )));

        result.AddRange(vacations.Select(v => new RecentRequestItem(
            v.Id,
            VacationType,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Status,
            $"{v.StartDate} - {v.EndDate}",
            v.CreatedAt
        )));

        return result.OrderByDescending(r => r.CreatedAt).Take(count).ToList();
    }
}
