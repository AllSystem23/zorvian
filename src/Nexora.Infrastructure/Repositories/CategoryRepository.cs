using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly NexoraDbContext _db;

    public CategoryRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<List<Category>> GetAllAsync(Guid companyId) =>
        await _db.Set<Category>()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(Guid id) =>
        await _db.Set<Category>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(Category category) =>
        await _db.Set<Category>().AddAsync(category);

    public Task UpdateAsync(Category category)
    {
        _db.Set<Category>().Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Category category)
    {
        _db.Set<Category>().Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
