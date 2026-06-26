using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CollectionActionRepository : ICollectionActionRepository
{
    private readonly ZorvianDbContext _db;

    public CollectionActionRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<CollectionAction?> GetByIdAsync(Guid id) =>
        await _db.Set<CollectionAction>()
            .Include(ca => ca.Employee)
            .FirstOrDefaultAsync(ca => ca.Id == id);

    public async Task<List<CollectionAction>> GetByCreditIdAsync(Guid creditId, int page, int pageSize) =>
        await _db.Set<CollectionAction>()
            .Include(ca => ca.Employee)
            .Where(ca => ca.CreditId == creditId)
            .OrderByDescending(ca => ca.ActionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetCountByCreditIdAsync(Guid creditId) =>
        await _db.Set<CollectionAction>().CountAsync(ca => ca.CreditId == creditId);

    public async Task<List<CollectionAction>> GetAllAsync(int page, int pageSize) =>
        await _db.Set<CollectionAction>()
            .Include(ca => ca.Employee)
            .OrderByDescending(ca => ca.ActionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<int> GetTotalCountAsync() =>
        await _db.Set<CollectionAction>().CountAsync();

    public async Task AddAsync(CollectionAction action) =>
        await _db.Set<CollectionAction>().AddAsync(action);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
