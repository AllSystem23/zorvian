using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IMaintenanceScheduleRepository
{
    Task<List<MaintenanceSchedule>> GetAllAsync(Guid companyId);
    Task<MaintenanceSchedule?> GetByIdAsync(Guid id);
    Task AddAsync(MaintenanceSchedule schedule);
    Task UpdateAsync(MaintenanceSchedule schedule);
    Task DeleteAsync(MaintenanceSchedule schedule);
    Task SaveChangesAsync();
}
