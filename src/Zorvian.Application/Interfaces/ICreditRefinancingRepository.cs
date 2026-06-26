using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICreditRefinancingRepository
{
    Task<List<CreditRefinancing>> GetByCreditIdAsync(Guid creditId);
    Task<List<CreditRefinancing>> GetAllAsync();
    Task AddAsync(CreditRefinancing refinancing);
    Task SaveChangesAsync();
}
