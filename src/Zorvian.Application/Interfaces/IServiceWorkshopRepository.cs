using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IServiceWorkshopRepository
{
    Task<List<ServiceWorkshop>> GetAllAsync(Guid companyId);
    Task<ServiceWorkshop?> GetByIdAsync(Guid id);
    Task AddAsync(ServiceWorkshop workshop);
    Task UpdateAsync(ServiceWorkshop workshop);
    Task DeleteAsync(ServiceWorkshop workshop);
    Task SaveChangesAsync();
}
