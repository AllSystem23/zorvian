using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISickLeaveRepository
{
    Task<List<SickLeaveRecord>> GetByEmployeeAsync(Guid employeeId);
    Task<SickLeaveRecord?> GetByIdAsync(Guid id);
    Task AddAsync(SickLeaveRecord record);
    Task UpdateAsync(SickLeaveRecord record);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
