using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyPartRequestRepository
{
    Task<List<WarrantyPartRequest>> GetByClaimIdAsync(Guid claimId);
    Task<List<WarrantyPartRequest>> GetByProviderIdAsync(Guid providerId);
    Task<WarrantyPartRequest?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantyPartRequest request);
    Task UpdateAsync(WarrantyPartRequest request);
    Task SaveChangesAsync();
}
