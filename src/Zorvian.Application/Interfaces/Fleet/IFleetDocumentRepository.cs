using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Interfaces.Fleet;

public interface IFleetDocumentRepository
{
    Task<List<FleetDocument>> GetAllAsync(Guid companyId);
    Task<List<FleetDocument>> GetByEntityAsync(string entityType, Guid entityId);
    Task<FleetDocument?> GetByIdAsync(Guid id);
    Task AddAsync(FleetDocument document);
    Task UpdateAsync(FleetDocument document);
    Task DeleteAsync(FleetDocument document);
    Task<List<FleetDocument>> GetExpiringAsync(int days);
    Task SaveChangesAsync();
}
