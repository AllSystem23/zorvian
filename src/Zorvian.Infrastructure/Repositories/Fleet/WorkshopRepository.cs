using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class WorkshopRepository : IWorkshopRepository
{
    private readonly ZorvianDbContext _db;

    public WorkshopRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Workshop>> GetAllAsync() =>
        await _db.Set<Workshop>().OrderBy(w => w.Name).ToListAsync();

    public async Task<Workshop?> GetByIdAsync(Guid id) =>
        await _db.Set<Workshop>().FirstOrDefaultAsync(w => w.Id == id);

    public async Task AddAsync(Workshop workshop) =>
        await _db.Set<Workshop>().AddAsync(workshop);

    public Task UpdateAsync(Workshop workshop)
    {
        _db.Set<Workshop>().Update(workshop);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Workshop workshop)
    {
        _db.Set<Workshop>().Remove(workshop);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
