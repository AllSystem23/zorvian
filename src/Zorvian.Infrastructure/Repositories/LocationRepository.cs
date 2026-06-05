using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class LocationRepository : ILocationRepository
{
    private readonly ZorvianDbContext _db;

    public LocationRepository(ZorvianDbContext db) => _db = db;

    public async Task<Location?> GetByIdAsync(Guid id) =>
        await _db.Set<Location>().FirstOrDefaultAsync(l => l.Id == id);

    public async Task<List<Location>> GetAllAsync(Guid companyId) =>
        await _db.Set<Location>()
            .Where(l => l.CompanyId == companyId && l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();

    public async Task AddAsync(Location location) => await _db.Set<Location>().AddAsync(location);
    public Task UpdateAsync(Location location) { _db.Set<Location>().Update(location); return Task.CompletedTask; }
    public Task DeleteAsync(Location location) { _db.Set<Location>().Remove(location); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
