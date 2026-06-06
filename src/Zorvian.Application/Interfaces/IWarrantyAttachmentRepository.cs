using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyAttachmentRepository
{
    Task<List<WarrantyAttachment>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<List<WarrantyAttachment>> GetByClaimIdAsync(Guid claimId);
    Task<WarrantyAttachment?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantyAttachment attachment);
    Task DeleteAsync(WarrantyAttachment attachment);
    Task SaveChangesAsync();
}
