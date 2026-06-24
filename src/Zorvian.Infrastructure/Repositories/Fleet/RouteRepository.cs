using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class RouteRepository : IRouteRepository
{
    private readonly ZorvianDbContext _db;

    public RouteRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Route>> GetAllAsync() =>
        await _db.Set<Route>()
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.CoDriver)
            .Include(r => r.Branch)
            .Include(r => r.Points)
            .OrderByDescending(r => r.ScheduledDate)
            .ToListAsync();

    public async Task<Route?> GetByIdAsync(Guid id) =>
        await _db.Set<Route>()
            .Include(r => r.Vehicle)
            .Include(r => r.Driver)
            .Include(r => r.CoDriver)
            .Include(r => r.Branch)
            .Include(r => r.Points)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(Route route) =>
        await _db.Set<Route>().AddAsync(route);

    public Task UpdateAsync(Route route)
    {
        _db.Set<Route>().Update(route);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Route route)
    {
        _db.Set<Route>().Remove(route);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
