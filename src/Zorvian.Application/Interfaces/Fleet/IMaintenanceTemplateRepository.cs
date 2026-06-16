using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IMaintenanceTemplateRepository
{
    Task<List<MaintenanceTemplate>> GetAllAsync();
    Task<MaintenanceTemplate?> GetByIdAsync(Guid id);
    Task AddAsync(MaintenanceTemplate template);
    Task UpdateAsync(MaintenanceTemplate template);
    Task DeleteAsync(MaintenanceTemplate template);
    Task SaveChangesAsync();
}
