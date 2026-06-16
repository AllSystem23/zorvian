using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFleetExpenseRepository
{
    Task<List<FleetExpense>> GetAllAsync(Guid companyId);
    Task<FleetExpense?> GetByIdAsync(Guid id);
    Task AddAsync(FleetExpense expense);
    Task UpdateAsync(FleetExpense expense);
    Task DeleteAsync(FleetExpense expense);
    Task SaveChangesAsync();
}
