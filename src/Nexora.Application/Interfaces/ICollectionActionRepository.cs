using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface ICollectionActionRepository
{
    Task<CollectionAction?> GetByIdAsync(Guid id);
    Task<List<CollectionAction>> GetByCreditIdAsync(Guid creditId, int page, int pageSize);
    Task<int> GetCountByCreditIdAsync(Guid creditId);
    Task AddAsync(CollectionAction action);
    Task SaveChangesAsync();
}
