using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFuelRefillRepository
{
    Task<List<FuelRefill>> GetAllAsync(Guid companyId, Guid? branchId = null);
    Task<FuelRefill?> GetByIdAsync(Guid id);
    Task AddAsync(FuelRefill fuelRefill);
    Task UpdateAsync(FuelRefill fuelRefill);
    Task DeleteAsync(FuelRefill fuelRefill);
    Task SaveChangesAsync();
}
