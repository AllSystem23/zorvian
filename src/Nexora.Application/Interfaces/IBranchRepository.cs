using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllAsync(Guid companyId);
    Task<Branch?> GetByIdAsync(Guid id);
    Task AddAsync(Branch branch);
    Task UpdateAsync(Branch branch);
    Task DeleteAsync(Branch branch);
    Task SaveChangesAsync();
}
