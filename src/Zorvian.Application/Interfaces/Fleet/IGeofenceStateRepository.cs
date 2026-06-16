using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IGeofenceStateRepository
{
    /// <summary>Get all active (IsInside=true) geofence states for a vehicle</summary>
    Task<List<VehicleGeofenceState>> GetActiveByVehicleAsync(Guid vehicleId);

    /// <summary>Get a specific active state for a vehicle + geofence pair</summary>
    Task<VehicleGeofenceState?> GetActiveByVehicleAndGeofenceAsync(Guid vehicleId, Guid geofenceId);

    /// <summary>Add a new geofence state</summary>
    Task AddAsync(VehicleGeofenceState state);

    /// <summary>Update an existing geofence state</summary>
    Task UpdateAsync(VehicleGeofenceState state);

    Task SaveChangesAsync();
}
