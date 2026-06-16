using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IExpenseCategoryRepository
{
    Task<List<ExpenseCategory>> GetAllAsync();
    Task<ExpenseCategory?> GetByIdAsync(Guid id);
    Task AddAsync(ExpenseCategory category);
    Task UpdateAsync(ExpenseCategory category);
    Task DeleteAsync(ExpenseCategory category);
    Task SaveChangesAsync();
}
