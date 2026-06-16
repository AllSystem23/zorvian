using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class VehicleTypeRepository : IVehicleTypeRepository
{
    private readonly ZorvianDbContext _db;

    public VehicleTypeRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<VehicleType>> GetAllAsync() =>
        await _db.Set<VehicleType>().OrderBy(t => t.Name).ToListAsync();

    public async Task<VehicleType?> GetByIdAsync(Guid id) =>
        await _db.Set<VehicleType>().FirstOrDefaultAsync(t => t.Id == id);

    public async Task AddAsync(VehicleType type) =>
        await _db.Set<VehicleType>().AddAsync(type);

    public Task UpdateAsync(VehicleType type)
    {
        _db.Set<VehicleType>().Update(type);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(VehicleType type)
    {
        _db.Set<VehicleType>().Remove(type);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
