using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class ExpenseSubcategoryRepository : IExpenseSubcategoryRepository
{
    private readonly ZorvianDbContext _db;

    public ExpenseSubcategoryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<ExpenseSubcategory>> GetAllAsync() =>
        await _db.Set<ExpenseSubcategory>().Include(s => s.Category).OrderBy(s => s.Name).ToListAsync();

    public async Task<List<ExpenseSubcategory>> GetByCategoryAsync(Guid categoryId) =>
        await _db.Set<ExpenseSubcategory>().Where(s => s.CategoryId == categoryId).OrderBy(s => s.Name).ToListAsync();

    public async Task<ExpenseSubcategory?> GetByIdAsync(Guid id) =>
        await _db.Set<ExpenseSubcategory>().Include(s => s.Category).FirstOrDefaultAsync(s => s.Id == id);

    public async Task AddAsync(ExpenseSubcategory subcategory) =>
        await _db.Set<ExpenseSubcategory>().AddAsync(subcategory);

    public Task UpdateAsync(ExpenseSubcategory subcategory)
    {
        _db.Set<ExpenseSubcategory>().Update(subcategory);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExpenseSubcategory subcategory)
    {
        _db.Set<ExpenseSubcategory>().Remove(subcategory);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
