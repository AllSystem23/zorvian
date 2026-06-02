using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class BrandRepository : IBrandRepository
{
    private readonly NexoraDbContext _db;

    public BrandRepository(NexoraDbContext db)
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
