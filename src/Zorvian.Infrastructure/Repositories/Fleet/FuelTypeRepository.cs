using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class FuelTypeRepository : IFuelTypeRepository
{
    private readonly ZorvianDbContext _db;

    public FuelTypeRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FuelType>> GetAllAsync() =>
        await _db.Set<FuelType>().OrderBy(f => f.Name).ToListAsync();

    public async Task<FuelType?> GetByIdAsync(Guid id) =>
        await _db.Set<FuelType>().FirstOrDefaultAsync(f => f.Id == id);

    public async Task AddAsync(FuelType fuelType) =>
        await _db.Set<FuelType>().AddAsync(fuelType);

    public Task UpdateAsync(FuelType fuelType)
    {
        _db.Set<FuelType>().Update(fuelType);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FuelType fuelType)
    {
        _db.Set<FuelType>().Remove(fuelType);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
