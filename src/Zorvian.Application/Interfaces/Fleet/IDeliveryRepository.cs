using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IDeliveryRepository
{
    Task<List<Delivery>> GetAllAsync(Guid? branchId = null);
    Task<Delivery?> GetByIdAsync(Guid id);
    Task AddAsync(Delivery delivery);
    Task UpdateAsync(Delivery delivery);
    Task DeleteAsync(Delivery delivery);
    Task SaveChangesAsync();
}
