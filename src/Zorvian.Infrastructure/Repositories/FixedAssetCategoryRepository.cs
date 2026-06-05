using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class FixedAssetCategoryRepository : IFixedAssetCategoryRepository
{
    private readonly ZorvianDbContext _db;

    public FixedAssetCategoryRepository(ZorvianDbContext db) => _db = db;

    public async Task<FixedAssetCategory?> GetByIdAsync(Guid id) =>
        await _db.Set<FixedAssetCategory>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<FixedAssetCategory>> GetAllAsync(Guid companyId) =>
        await _db.Set<FixedAssetCategory>()
            .Where(c => c.CompanyId == companyId && c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task AddAsync(FixedAssetCategory category) => await _db.Set<FixedAssetCategory>().AddAsync(category);
    public Task UpdateAsync(FixedAssetCategory category) { _db.Set<FixedAssetCategory>().Update(category); return Task.CompletedTask; }
    public Task DeleteAsync(FixedAssetCategory category) { _db.Set<FixedAssetCategory>().Remove(category); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
