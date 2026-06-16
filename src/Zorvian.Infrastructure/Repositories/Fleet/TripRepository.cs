using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class TripRepository : ITripRepository
{
    private readonly ZorvianDbContext _db;

    public TripRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Trip>> GetAllAsync() =>
        await _db.Set<Trip>()
            .Include(t => t.Vehicle)
            .Include(t => t.Driver)
            .Include(t => t.CoDriver)
            .OrderByDescending(t => t.StartDateTime)
            .ToListAsync();

    public async Task<Trip?> GetByIdAsync(Guid id) =>
        await _db.Set<Trip>()
            .Include(t => t.Vehicle)
            .Include(t => t.Driver)
            .Include(t => t.CoDriver)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(Trip trip) =>
        await _db.Set<Trip>().AddAsync(trip);

    public Task UpdateAsync(Trip trip)
    {
        _db.Set<Trip>().Update(trip);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Trip trip)
    {
        _db.Set<Trip>().Remove(trip);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
