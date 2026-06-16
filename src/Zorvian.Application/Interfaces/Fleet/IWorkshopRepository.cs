using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IWorkshopRepository
{
    Task<List<Workshop>> GetAllAsync();
    Task<Workshop?> GetByIdAsync(Guid id);
    Task AddAsync(Workshop workshop);
    Task UpdateAsync(Workshop workshop);
    Task DeleteAsync(Workshop workshop);
    Task SaveChangesAsync();
}
