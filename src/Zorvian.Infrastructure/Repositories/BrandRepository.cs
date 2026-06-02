using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly ZorvianDbContext _db;

    public BrandRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<Brand>> GetAllAsync(Guid companyId) =>
        await _db.Set<Brand>()
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .ToListAsync();

    public async Task<Brand?> GetByIdAsync(Guid id) =>
        await _db.Set<Brand>().FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(Brand brand) =>
        await _db.Set<Brand>().AddAsync(brand);

    public Task UpdateAsync(Brand brand)
    {
        _db.Set<Brand>().Update(brand);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Brand brand)
    {
        _db.Set<Brand>().Remove(brand);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
