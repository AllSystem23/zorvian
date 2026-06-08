using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IBudgetRepository
{
    Task<List<Budget>> GetAllAsync(Guid companyId);
    Task<List<Budget>> GetByPeriodAsync(int year, int month, Guid companyId);
    Task<Budget?> GetByIdAsync(Guid id);
    Task AddAsync(Budget budget);
    Task UpdateAsync(Budget budget);
    Task DeleteAsync(Budget budget);
    Task SaveChangesAsync();
}
