using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFailureTypeRepository
{
    Task<List<FailureType>> GetAllAsync();
    Task<FailureType?> GetByIdAsync(Guid id);
    Task AddAsync(FailureType failureType);
    Task UpdateAsync(FailureType failureType);
    Task DeleteAsync(FailureType failureType);
    Task SaveChangesAsync();
}
