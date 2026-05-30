using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

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
}
