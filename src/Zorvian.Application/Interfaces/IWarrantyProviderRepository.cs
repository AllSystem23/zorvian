using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyProviderRepository
{
    Task<List<WarrantyProvider>> GetAllAsync(Guid companyId);
    Task<WarrantyProvider?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantyProvider provider);
    Task UpdateAsync(WarrantyProvider provider);
    Task DeleteAsync(WarrantyProvider provider);
    Task SaveChangesAsync();
}
