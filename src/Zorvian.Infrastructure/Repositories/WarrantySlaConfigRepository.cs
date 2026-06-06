using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantySlaConfigRepository : IWarrantySlaConfigRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantySlaConfigRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<WarrantySlaConfig>> GetAllAsync() =>
        await _db.Set<WarrantySlaConfig>().ToListAsync();

    public async Task<WarrantySlaConfig?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantySlaConfig>().FindAsync(id);

    public async Task AddAsync(WarrantySlaConfig slaConfig) =>
        await _db.Set<WarrantySlaConfig>().AddAsync(slaConfig);

    public Task UpdateAsync(WarrantySlaConfig slaConfig)
    {
        _db.Set<WarrantySlaConfig>().Update(slaConfig);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(WarrantySlaConfig slaConfig)
    {
        _db.Set<WarrantySlaConfig>().Remove(slaConfig);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
