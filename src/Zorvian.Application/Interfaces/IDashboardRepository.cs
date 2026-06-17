using Zorvian.Application.DTOs.Dashboard;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDashboardRepository
{
    // Ultra-optimized: single raw SQL for ALL scalar KPI values (1 round-trip)
    Task<DashboardKpiScalars> GetAllKpiScalarsRawAsync(string tenantId, bool isSuperAdmin, DateOnly thirtyDaysAgo, int currentMonth, int lastMonth);
    Task<ExecutiveKpiScalars> GetExecutiveKpiScalarsRawAsync(string tenantId, bool isSuperAdmin);
    Task<List<(string Name, int Count)>> GetEmployeesByDepartmentAsync();
    Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end);
    Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count);
    Task<List<VacationRequest>> GetRecentVacationsAsync(int count);

    // Used by individual endpoints and executive dashboard
    Task<int> GetTotalEmployeesAsync();
    Task<int> GetActiveEmployeesAsync();
    Task<int> GetPendingVacationRequestsAsync();
    Task<int> GetPendingPermissionRequestsAsync();

    // Payroll Dashboard
    Task<List<(string Department, decimal Amount)>> GetPayrollCostByDepartmentAsync();
    Task<List<(string Period, decimal Amount)>> GetPayrollHistoryAsync(int count);
}
