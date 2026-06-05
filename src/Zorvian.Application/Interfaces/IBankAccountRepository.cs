using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IBankAccountRepository
{
    Task<List<EmployeeBankAccount>> GetByEmployeeIdAsync(Guid employeeId);
    Task<EmployeeBankAccount?> GetByIdAsync(Guid id);
    Task AddAsync(EmployeeBankAccount account);
    Task UpdateAsync(EmployeeBankAccount account);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
