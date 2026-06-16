using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IWorkOrderRepository
{
    Task<List<WorkOrder>> GetAllAsync(Guid companyId);
    Task<WorkOrder?> GetByIdAsync(Guid id);
    Task AddAsync(WorkOrder workOrder);
    Task UpdateAsync(WorkOrder workOrder);
    Task DeleteAsync(WorkOrder workOrder);
    Task SaveChangesAsync();
}
