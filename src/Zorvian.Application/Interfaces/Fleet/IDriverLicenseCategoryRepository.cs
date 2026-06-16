using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IDriverLicenseCategoryRepository
{
    Task<List<DriverLicenseCategory>> GetAllAsync();
    Task<DriverLicenseCategory?> GetByIdAsync(Guid id);
    Task AddAsync(DriverLicenseCategory category);
    Task UpdateAsync(DriverLicenseCategory category);
    Task DeleteAsync(DriverLicenseCategory category);
    Task SaveChangesAsync();
}
