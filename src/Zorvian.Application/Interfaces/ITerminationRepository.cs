using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ITerminationRepository
{
    Task<TerminationRecord?> GetByIdAsync(Guid id);
    Task<List<TerminationRecord>> GetByEmployeeAsync(Guid employeeId);
    Task AddAsync(TerminationRecord record);
    Task UpdateAsync(TerminationRecord record);
    Task SaveChangesAsync();
}
