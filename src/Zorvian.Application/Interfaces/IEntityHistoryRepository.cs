using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IEntityHistoryRepository
{
    Task AddRangeAsync(IEnumerable<EntityHistory> entries);
    Task SaveChangesAsync();
}
