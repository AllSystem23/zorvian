using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICollectionActionRepository
{
    Task<CollectionAction?> GetByIdAsync(Guid id);
    Task<List<CollectionAction>> GetByCreditIdAsync(Guid creditId, int page, int pageSize);
    Task<int> GetCountByCreditIdAsync(Guid creditId);
    Task<List<CollectionAction>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task AddAsync(CollectionAction action);
    Task SaveChangesAsync();
}
