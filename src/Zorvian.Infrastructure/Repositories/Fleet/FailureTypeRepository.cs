using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class FailureTypeRepository : IFailureTypeRepository
{
    private readonly ZorvianDbContext _db;

    public FailureTypeRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FailureType>> GetAllAsync() =>
        await _db.Set<FailureType>().OrderBy(f => f.Name).ToListAsync();

    public async Task<FailureType?> GetByIdAsync(Guid id) =>
        await _db.Set<FailureType>().FirstOrDefaultAsync(f => f.Id == id);

    public async Task AddAsync(FailureType failureType) =>
        await _db.Set<FailureType>().AddAsync(failureType);

    public Task UpdateAsync(FailureType failureType)
    {
        _db.Set<FailureType>().Update(failureType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FailureType failureType)
    {
        _db.Set<FailureType>().Remove(failureType);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
