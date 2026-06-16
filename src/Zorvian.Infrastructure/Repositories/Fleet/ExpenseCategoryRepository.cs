using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly ZorvianDbContext _db;

    public ExpenseCategoryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<ExpenseCategory>> GetAllAsync() =>
        await _db.Set<ExpenseCategory>().OrderBy(c => c.Name).ToListAsync();

    public async Task<ExpenseCategory?> GetByIdAsync(Guid id) =>
        await _db.Set<ExpenseCategory>().FirstOrDefaultAsync(c => c.Id == id);

    public async Task AddAsync(ExpenseCategory category) =>
        await _db.Set<ExpenseCategory>().AddAsync(category);

    public Task UpdateAsync(ExpenseCategory category)
    {
        _db.Set<ExpenseCategory>().Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExpenseCategory category)
    {
        _db.Set<ExpenseCategory>().Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
