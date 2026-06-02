using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(Guid companyId);
    Task<Category?> GetByIdAsync(Guid id);
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Category category);
    Task SaveChangesAsync();
}
