using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IVehicleBrandRepository
{
    Task<List<VehicleBrand>> GetAllAsync();
    Task<VehicleBrand?> GetByIdAsync(Guid id);
    Task AddAsync(VehicleBrand brand);
    Task UpdateAsync(VehicleBrand brand);
    Task DeleteAsync(VehicleBrand brand);
    Task SaveChangesAsync();
}
