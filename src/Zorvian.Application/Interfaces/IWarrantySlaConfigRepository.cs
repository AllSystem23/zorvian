using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantySlaConfigRepository
{
    Task<List<WarrantySlaConfig>> GetAllAsync();
    Task<WarrantySlaConfig?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantySlaConfig slaConfig);
    Task UpdateAsync(WarrantySlaConfig slaConfig);
    Task DeleteAsync(WarrantySlaConfig slaConfig);
    Task SaveChangesAsync();
}
