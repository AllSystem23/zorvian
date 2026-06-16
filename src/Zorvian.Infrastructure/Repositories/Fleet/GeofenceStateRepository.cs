using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class GeofenceStateRepository : IGeofenceStateRepository
{
    private readonly ZorvianDbContext _db;

    public GeofenceStateRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<VehicleGeofenceState>> GetActiveByVehicleAsync(Guid vehicleId) =>
        await _db.VehicleGeofenceStates
            .Where(s => s.VehicleId == vehicleId && s.IsInside)
            .ToListAsync();

    public async Task<VehicleGeofenceState?> GetActiveByVehicleAndGeofenceAsync(Guid vehicleId, Guid geofenceId) =>
        await _db.VehicleGeofenceStates
            .FirstOrDefaultAsync(s => s.VehicleId == vehicleId && s.GeofenceId == geofenceId && s.IsInside);

    public async Task AddAsync(VehicleGeofenceState state) =>
        await _db.VehicleGeofenceStates.AddAsync(state);

    public Task UpdateAsync(VehicleGeofenceState state)
    {
        _db.VehicleGeofenceStates.Update(state);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
