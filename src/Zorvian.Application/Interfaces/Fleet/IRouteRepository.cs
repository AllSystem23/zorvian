using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IRouteRepository
{
    Task<List<Route>> GetAllAsync();
    Task<Route?> GetByIdAsync(Guid id);
    Task AddAsync(Route route);
    Task UpdateAsync(Route route);
    Task DeleteAsync(Route route);
    Task SaveChangesAsync();
}
