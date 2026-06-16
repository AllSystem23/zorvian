using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IExpenseSubcategoryRepository
{
    Task<List<ExpenseSubcategory>> GetAllAsync();
    Task<List<ExpenseSubcategory>> GetByCategoryAsync(Guid categoryId);
    Task<ExpenseSubcategory?> GetByIdAsync(Guid id);
    Task AddAsync(ExpenseSubcategory subcategory);
    Task UpdateAsync(ExpenseSubcategory subcategory);
    Task DeleteAsync(ExpenseSubcategory subcategory);
    Task SaveChangesAsync();
}
