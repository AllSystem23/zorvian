using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class EntityHistoryRepository : IEntityHistoryRepository
{
    private readonly ZorvianDbContext _db;

    public EntityHistoryRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task AddRangeAsync(IEnumerable<EntityHistory> entries)
    {
        await _db.EntityHistories.AddRangeAsync(entries);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
