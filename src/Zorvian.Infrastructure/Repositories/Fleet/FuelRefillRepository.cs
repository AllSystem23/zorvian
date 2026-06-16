using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class FuelRefillRepository : IFuelRefillRepository
{
    private readonly ZorvianDbContext _db;

    public FuelRefillRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FuelRefill>> GetAllAsync(Guid companyId) =>
        await _db.Set<FuelRefill>()
            .Include(f => f.Vehicle)
            .Include(f => f.Driver)
            .Include(f => f.FuelType)
            .Include(f => f.Supplier)
            .Where(f => f.TenantId == companyId.ToString())
            .OrderByDescending(f => f.RefillDateTime)
            .ToListAsync();

    public async Task<FuelRefill?> GetByIdAsync(Guid id) =>
        await _db.Set<FuelRefill>()
            .Include(f => f.Vehicle)
            .Include(f => f.Driver)
            .Include(f => f.FuelType)
            .Include(f => f.Supplier)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task AddAsync(FuelRefill fuelRefill) =>
        await _db.Set<FuelRefill>().AddAsync(fuelRefill);

    public Task UpdateAsync(FuelRefill fuelRefill)
    {
        _db.Set<FuelRefill>().Update(fuelRefill);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FuelRefill fuelRefill)
    {
        _db.Set<FuelRefill>().Remove(fuelRefill);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
