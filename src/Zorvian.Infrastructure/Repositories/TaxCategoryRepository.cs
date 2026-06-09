using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class TaxCategoryRepository : ITaxCategoryRepository
{
    private readonly ZorvianDbContext _db;

    public TaxCategoryRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<TaxCategory?> GetByIdAsync(Guid id)
    {
        return await _db.TaxCategories.FindAsync(id);
    }

    public async Task<List<TaxCategory>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _db.TaxCategories
            .Where(t => t.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task AddAsync(TaxCategory taxCategory)
    {
        await _db.TaxCategories.AddAsync(taxCategory);
    }

    public async Task UpdateAsync(TaxCategory taxCategory)
    {
        _db.TaxCategories.Update(taxCategory);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(TaxCategory taxCategory)
    {
        taxCategory.IsDeleted = true;
        taxCategory.DeletedAt = DateTime.UtcNow;
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
