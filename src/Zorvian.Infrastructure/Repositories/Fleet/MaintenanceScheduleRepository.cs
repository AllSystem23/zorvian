using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class MaintenanceScheduleRepository : IMaintenanceScheduleRepository
{
    private readonly ZorvianDbContext _db;

    public MaintenanceScheduleRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<MaintenanceSchedule>> GetAllAsync(Guid companyId) =>
        await _db.Set<MaintenanceSchedule>()
            .Include(s => s.Vehicle)
            .Include(s => s.Template)
            .Where(s => s.TenantId == companyId.ToString())
            .OrderByDescending(s => s.NextExecutionDate)
            .ToListAsync();

    public async Task<MaintenanceSchedule?> GetByIdAsync(Guid id) =>
        await _db.Set<MaintenanceSchedule>()
            .Include(s => s.Vehicle)
            .Include(s => s.Template)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(MaintenanceSchedule schedule) =>
        await _db.Set<MaintenanceSchedule>().AddAsync(schedule);

    public Task UpdateAsync(MaintenanceSchedule schedule)
    {
        _db.Set<MaintenanceSchedule>().Update(schedule);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MaintenanceSchedule schedule)
    {
        _db.Set<MaintenanceSchedule>().Remove(schedule);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
