using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class MaintenanceTemplateRepository : IMaintenanceTemplateRepository
{
    private readonly ZorvianDbContext _db;

    public MaintenanceTemplateRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<MaintenanceTemplate>> GetAllAsync() =>
        await _db.Set<MaintenanceTemplate>().OrderBy(t => t.Name).ToListAsync();

    public async Task<MaintenanceTemplate?> GetByIdAsync(Guid id) =>
        await _db.Set<MaintenanceTemplate>().FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(MaintenanceTemplate template) =>
        await _db.Set<MaintenanceTemplate>().AddAsync(template);

    public Task UpdateAsync(MaintenanceTemplate template)
    {
        _db.Set<MaintenanceTemplate>().Update(template);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MaintenanceTemplate template)
    {
        _db.Set<MaintenanceTemplate>().Remove(template);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
