using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id);
    Task<List<Employee>> GetFilteredAsync(string? search, string? status, Guid? departmentId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(string? search, string? status, Guid? departmentId);
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task DeleteAsync(Employee employee);
    Task SaveChangesAsync();
}
