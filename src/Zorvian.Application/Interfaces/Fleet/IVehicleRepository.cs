using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IVehicleRepository
{
    Task<List<Vehicle>> GetAllAsync(Guid companyId);
    Task<Vehicle?> GetByIdAsync(Guid id);
    Task AddAsync(Vehicle vehicle);
    Task UpdateAsync(Vehicle vehicle);
    Task DeleteAsync(Vehicle vehicle);
    Task SaveChangesAsync();
}
