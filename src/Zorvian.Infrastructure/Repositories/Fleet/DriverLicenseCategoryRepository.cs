using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class DriverLicenseCategoryRepository : IDriverLicenseCategoryRepository
{
    private readonly ZorvianDbContext _db;

    public DriverLicenseCategoryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<DriverLicenseCategory>> GetAllAsync() =>
        await _db.Set<DriverLicenseCategory>().OrderBy(c => c.Name).ToListAsync();

    public async Task<DriverLicenseCategory?> GetByIdAsync(Guid id) =>
        await _db.Set<DriverLicenseCategory>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(DriverLicenseCategory category) =>
        await _db.Set<DriverLicenseCategory>().AddAsync(category);

    public Task UpdateAsync(DriverLicenseCategory category)
    {
        _db.Set<DriverLicenseCategory>().Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DriverLicenseCategory category)
    {
        _db.Set<DriverLicenseCategory>().Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
