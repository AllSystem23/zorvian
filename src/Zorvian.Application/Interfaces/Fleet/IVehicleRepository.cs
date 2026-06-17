using Zorvian.Application.DTOs.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IVehicleRepository
{
    // Ultra-optimized: single raw SQL for ALL fleet dashboard counts (1 round-trip)
    Task<FleetDashboardScalars> GetDashboardScalarsRawAsync(string tenantId, bool isSuperAdmin);

    Task<List<Vehicle>> GetAllAsync(Guid companyId);
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Vehicle vehicle);
    Task SaveChangesAsync();
}
