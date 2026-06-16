using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class VehicleBrandRepository : IVehicleBrandRepository
{
    private readonly ZorvianDbContext _db;

    public VehicleBrandRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<VehicleBrand>> GetAllAsync() =>
        await _db.Set<VehicleBrand>().OrderBy(b => b.Name).ToListAsync();

    public async Task<VehicleBrand?> GetByIdAsync(Guid id) =>
        await _db.Set<VehicleBrand>().FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(VehicleBrand brand) =>
        await _db.Set<VehicleBrand>().AddAsync(brand);

    public Task UpdateAsync(VehicleBrand brand)
    {
        _db.Set<VehicleBrand>().Update(brand);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(VehicleBrand brand)
    {
        _db.Set<VehicleBrand>().Remove(brand);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
