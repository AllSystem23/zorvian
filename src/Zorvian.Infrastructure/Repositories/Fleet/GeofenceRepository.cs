using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class GeofenceRepository : IGeofenceRepository
{
    private readonly ZorvianDbContext _db;

    public GeofenceRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Geofence>> GetAllAsync() =>
        await _db.Set<Geofence>()
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

    public async Task<Geofence?> GetByIdAsync(Guid id) =>
        await _db.Set<Geofence>().FirstOrDefaultAsync(g => g.Id == id);

    public async Task<List<Geofence>> GetActiveAsync() =>
        await _db.Set<Geofence>()
            .Where(g => g.Active)
            .ToListAsync();

    public async Task AddAsync(Geofence geofence) =>
        await _db.Set<Geofence>().AddAsync(geofence);

    public Task UpdateAsync(Geofence geofence)
    {
        _db.Set<Geofence>().Update(geofence);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Geofence geofence)
    {
        _db.Set<Geofence>().Remove(geofence);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
