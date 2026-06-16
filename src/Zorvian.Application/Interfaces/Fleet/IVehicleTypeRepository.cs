using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IVehicleTypeRepository
{
    Task<List<VehicleType>> GetAllAsync();
    Task<VehicleType?> GetByIdAsync(Guid id);
    Task AddAsync(VehicleType type);
    Task UpdateAsync(VehicleType type);
    Task DeleteAsync(VehicleType type);
    Task SaveChangesAsync();
}
