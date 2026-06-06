using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyStateHistoryRepository
{
    Task<List<WarrantyStateHistory>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<List<WarrantyStateHistory>> GetByClaimIdAsync(Guid claimId);
    Task AddAsync(WarrantyStateHistory history);
    Task SaveChangesAsync();
}
