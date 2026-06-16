using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface ITripRepository
{
    Task<List<Trip>> GetAllAsync();
    Task<Trip?> GetByIdAsync(Guid id);
    Task AddAsync(Trip trip);
    Task UpdateAsync(Trip trip);
    Task DeleteAsync(Trip trip);
    Task SaveChangesAsync();
}
