using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id);
    Task<List<Department>> GetAllAsync();
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(Department department);
    Task<bool> HasEmployeesAsync(Guid departmentId);
    Task SaveChangesAsync();
}
