using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IGeofenceRepository
{
    Task<List<Geofence>> GetAllAsync();
    Task<Geofence?> GetByIdAsync(Guid id);
    Task<List<Geofence>> GetActiveAsync();
    Task AddAsync(Geofence geofence);
    Task UpdateAsync(Geofence geofence);
    Task DeleteAsync(Geofence geofence);
    Task SaveChangesAsync();
}
