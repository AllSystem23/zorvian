using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class VehicleRepository : IVehicleRepository
{
    private readonly ZorvianDbContext _db;

    public VehicleRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Vehicle>> GetAllAsync(Guid companyId) =>
        await _db.Set<Vehicle>()
            .Include(v => v.Brand)
            .Include(v => v.VehicleType)
            .Include(v => v.FuelType)
            .Include(v => v.Branch)
            .Include(v => v.Driver)
            .OrderBy(v => v.Code)
            .ToListAsync();

    public async Task<Vehicle?> GetByIdAsync(Guid id) =>
        await _db.Set<Vehicle>()
            .Include(v => v.Brand)
            .Include(v => v.VehicleType)
            .Include(v => v.FuelType)
            .Include(v => v.Branch)
            .Include(v => v.Driver)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task AddAsync(Vehicle vehicle) =>
        await _db.Set<Vehicle>().AddAsync(vehicle);

    public Task UpdateAsync(Vehicle vehicle)
    {
        _db.Set<Vehicle>().Update(vehicle);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Vehicle vehicle)
    {
        _db.Set<Vehicle>().Remove(vehicle);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
