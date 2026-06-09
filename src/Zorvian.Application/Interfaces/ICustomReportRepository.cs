using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICustomReportRepository
{
    Task<List<CustomReport>> GetAllAsync(Guid companyId);
    Task<CustomReport?> GetByIdAsync(Guid id);
    Task<List<CustomReport>> GetByModuleAsync(string module, Guid companyId);
    Task<CustomReport> AddAsync(CustomReport report);
    Task<CustomReport?> UpdateAsync(CustomReport report);
    Task<bool> DeleteAsync(Guid id);
}
