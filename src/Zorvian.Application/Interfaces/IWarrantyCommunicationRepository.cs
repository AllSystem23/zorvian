using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarrantyCommunicationRepository
{
    Task<List<WarrantyCommunication>> GetByWarrantyIdAsync(Guid warrantyId);
    Task<WarrantyCommunication?> GetByIdAsync(Guid id);
    Task AddAsync(WarrantyCommunication communication);
    Task SaveChangesAsync();
}
