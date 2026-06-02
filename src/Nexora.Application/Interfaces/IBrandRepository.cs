using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IBrandRepository
{
    Task<List<Brand>> GetAllAsync(Guid companyId);
    Task<Brand?> GetByIdAsync(Guid id);
    Task AddAsync(Brand brand);
    Task UpdateAsync(Brand brand);
    Task DeleteAsync(Brand brand);
    Task SaveChangesAsync();
}
