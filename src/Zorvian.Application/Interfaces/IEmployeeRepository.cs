using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id);
    Task<Employee?> GetByEmployeeCodeAsync(string code);
    Task<List<Employee>> SearchByCodeAsync(string partialCode, int maxResults);
    Task<List<Employee>> GetFilteredAsync(string? search, string? status, Guid? departmentId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, Guid? departmentId);
    Task<List<EmployeeSupervisor>> GetSupervisorsAsync(Guid employeeId);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Employee employee);
    Task SaveChangesAsync();

    // Extensions for Payroll
    Task<List<AttendanceRecord>> GetAttendanceInRangeAsync(Guid employeeId, DateOnly start, DateOnly end);
    Task<List<VacationRequest>> GetVacationsInRangeAsync(Guid employeeId, DateOnly start, DateOnly end);
    Task<List<EmployeeBankAccount>> GetBankAccountsAsync(Guid employeeId);
}
