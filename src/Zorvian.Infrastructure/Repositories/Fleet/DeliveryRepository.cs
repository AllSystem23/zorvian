using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

#nullable enable
#pragma warning disable CS8602 // Los selectores de sucursal transitiva los guarda el helper (nav null-check)

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class DeliveryRepository : IDeliveryRepository
{
    private readonly ZorvianDbContext _db;

    public DeliveryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Delivery>> GetAllAsync(Guid? branchId = null) =>
        await _db.Set<Delivery>()
            .Include(d => d.Client)
            .Include(d => d.Vehicle)
            .Include(d => d.Driver)
            .Include(d => d.Route)
            .Include(d => d.Items)
            .ApplyBranchScopeTransitive(
                branchId,
                d => d.VehicleId == null && d.RouteId == null,
                d => d.Vehicle.BranchId,
                d => d.Route.BranchId)
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
