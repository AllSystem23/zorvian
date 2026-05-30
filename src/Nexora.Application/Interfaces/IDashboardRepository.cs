using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IDashboardRepository
{
    Task<int> GetTotalEmployeesAsync();
    Task<int> GetActiveEmployeesAsync();
    Task<int> GetInactiveEmployeesAsync();
    Task<List<(string Name, int Count)>> GetEmployeesByDepartmentAsync();
    Task<int> GetPendingVacationRequestsAsync();
    Task<int> GetPendingPermissionRequestsAsync();
    Task<List<Employee>> GetEmployeesWithBirthdayThisMonthAsync();
    Task<List<Employee>> GetEmployeesWithAnniversaryThisMonthAsync();
    Task<List<VacationRequest>> GetVacationsInRangeAsync(DateOnly start, DateOnly end);
    Task<List<PermissionRequest>> GetRecentPermissionsAsync(int count);
    Task<List<VacationRequest>> GetRecentVacationsAsync(int count);
}
