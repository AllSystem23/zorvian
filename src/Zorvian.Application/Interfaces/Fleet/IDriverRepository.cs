using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IDriverRepository
{
    Task<List<Driver>> GetAllAsync(Guid companyId);
    Task<Driver?> GetByIdAsync(Guid id);
    Task AddAsync(Driver driver);
    Task UpdateAsync(Driver driver);
    Task DeleteAsync(Driver driver);
    Task SaveChangesAsync();
}
