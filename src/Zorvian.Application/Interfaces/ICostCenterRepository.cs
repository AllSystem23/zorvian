using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICostCenterRepository
{
    Task<List<CostCenter>> GetAllAsync(Guid companyId);
    Task<CostCenter?> GetByIdAsync(Guid id);
    Task AddAsync(CostCenter costCenter);
    Task UpdateAsync(CostCenter costCenter);
    Task DeleteAsync(CostCenter costCenter);
    Task SaveChangesAsync();
}
