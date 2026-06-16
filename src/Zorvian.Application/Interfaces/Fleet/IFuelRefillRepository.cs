using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFuelRefillRepository
{
    Task<List<FuelRefill>> GetAllAsync(Guid companyId);
    Task<FuelRefill?> GetByIdAsync(Guid id);
    Task AddAsync(FuelRefill fuelRefill);
    Task UpdateAsync(FuelRefill fuelRefill);
    Task DeleteAsync(FuelRefill fuelRefill);
    Task SaveChangesAsync();
}
