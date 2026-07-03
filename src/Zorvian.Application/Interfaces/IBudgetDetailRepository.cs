using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IBudgetDetailRepository
{
    Task<BudgetDetail?> GetByIdAsync(Guid id);
    Task<List<BudgetDetail>> GetByBudgetIdAsync(Guid budgetId);
    Task<List<BudgetDetail>> GetByPeriodAsync(int year, int month, Guid companyId);
    Task<List<BudgetDetail>> GetFilteredAsync(Guid? budgetId, Guid? accountId, int? year, int? month, Guid companyId);
    Task AddAsync(BudgetDetail detail);
    Task UpdateAsync(BudgetDetail detail);
    Task DeleteAsync(BudgetDetail detail);
    Task SaveChangesAsync();
}

public interface IBudgetTrackingRepository
{
    Task<BudgetTracking?> GetByIdAsync(Guid id);
    Task<List<BudgetTracking>> GetByBudgetDetailIdAsync(Guid budgetDetailId);
    Task<List<BudgetTracking>> GetFilteredAsync(Guid? budgetDetailId, int? year, int? month, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? budgetDetailId, int? year, int? month, Guid companyId);
    Task AddAsync(BudgetTracking tracking);
    Task UpdateAsync(BudgetTracking tracking);
    Task DeleteAsync(BudgetTracking tracking);
    Task SaveChangesAsync();
    Task<BudgetTracking?> GetByDetailAndPeriodAsync(Guid budgetDetailId, int month, int year);
}
