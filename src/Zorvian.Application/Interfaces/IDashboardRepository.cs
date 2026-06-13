using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetTotalEmployeesAsync();
    Task<int> GetActiveEmployeesAsync();
    Task<int> GetPreviousMonthActiveEmployeesAsync();
    Task<int> GetInactiveEmployeesAsync();
    Task<List<(string Name, int Count)>> GetEmployeesByDepartmentAsync();
    Task<int> GetPendingVacationRequestsAsync();
    Task<int> GetPendingPermissionRequestsAsync();
    Task<int> GetResolvedVacationCountSinceAsync(DateOnly since);
    Task<int> GetResolvedPermissionCountSinceAsync(DateOnly since);
    Task<List<Employee>> GetEmployeesWithBirthdayThisMonthAsync();
    Task<List<Employee>> GetEmployeesWithBirthdayInMonthAsync(int month);
    Task<List<Employee>> GetEmployeesWithAnniversaryThisMonthAsync();
    Task<List<Employee>> GetEmployeesWithAnniversaryInMonthAsync(int month);
    Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end);
    Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count);
    Task<List<VacationRequest>> GetRecentVacationsAsync(int count);
    
    // Payroll Dashboard
    Task<List<(string Department, decimal Amount)>> GetPayrollCostByDepartmentAsync();
    Task<List<(string Period, decimal Amount)>> GetPayrollHistoryAsync(int count);
}
