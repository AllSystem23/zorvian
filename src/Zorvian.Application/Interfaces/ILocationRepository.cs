using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ILocationRepository
{
    Task<Location?> GetByIdAsync(Guid id);
    Task<List<Location>> GetAllAsync(Guid companyId);
    Task AddAsync(Location location);
    Task UpdateAsync(Location location);
    Task DeleteAsync(Location location);
    Task SaveChangesAsync();
}
