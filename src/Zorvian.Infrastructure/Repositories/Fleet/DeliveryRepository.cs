using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class DeliveryRepository : IDeliveryRepository
{
    private readonly ZorvianDbContext _db;

    public DeliveryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Delivery>> GetAllAsync() =>
        await _db.Set<Delivery>()
            .Include(d => d.Client)
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Route)
            .Include(d => d.Items)
            .OrderByDescending(d => d.ScheduledDate)
            .ToListAsync();

    public async Task<Delivery?> GetByIdAsync(Guid id) =>
        await _db.Set<Delivery>()
            .Include(d => d.Client)
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Route)
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Delivery delivery) =>
        await _db.Set<Delivery>().AddAsync(delivery);

    public Task UpdateAsync(Delivery delivery)
    {
        _db.Set<Delivery>().Update(delivery);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Delivery delivery)
    {
        _db.Set<Delivery>().Remove(delivery);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
