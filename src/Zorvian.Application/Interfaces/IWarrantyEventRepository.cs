using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyEventRepository
{
    Task<List<WarrantyEvent>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<List<WarrantyEvent>> GetMilestonesAsync(Guid warrantyId);
    Task AddAsync(WarrantyEvent evt);
    Task SaveChangesAsync();
}
