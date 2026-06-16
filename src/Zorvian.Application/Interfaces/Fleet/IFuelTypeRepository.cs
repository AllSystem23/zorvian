using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFuelTypeRepository
{
    Task<List<FuelType>> GetAllAsync();
    Task<FuelType?> GetByIdAsync(Guid id);
    Task AddAsync(FuelType fuelType);
    Task UpdateAsync(FuelType fuelType);
    Task DeleteAsync(FuelType fuelType);
    Task SaveChangesAsync();
}
