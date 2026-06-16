using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class DriverRepository : IDriverRepository
{
    private readonly ZorvianDbContext _db;

    public DriverRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Driver>> GetAllAsync(Guid companyId) =>
        await _db.Set<Driver>()
            .Include(d => d.LicenseCategory)
            .Include(d => d.Branch)
            .OrderBy(d => d.FirstName)
            .ToListAsync();

    public async Task<Driver?> GetByIdAsync(Guid id) =>
        await _db.Set<Driver>()
            .Include(d => d.LicenseCategory)
            .Include(d => d.Branch)
            .FirstOrDefaultAsync(d => d.Id == id);

    public async Task AddAsync(Driver driver) =>
        await _db.Set<Driver>().AddAsync(driver);

    public Task UpdateAsync(Driver driver)
    {
        _db.Set<Driver>().Update(driver);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Driver driver)
    {
        _db.Set<Driver>().Remove(driver);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
