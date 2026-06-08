using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BudgetRepository : IBudgetRepository
{
    private readonly ZorvianDbContext _db;

    public BudgetRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<Budget>> GetAllAsync(Guid companyId) =>
        await _db.Set<Budget>()
            .Include(b => b.Account)
            .Include(b => b.CostCenter)
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Year).ThenBy(b => b.Month).ThenBy(b => b.Account.Code)
            .ToListAsync();

    public async Task<List<Budget>> GetByPeriodAsync(int year, int month, Guid companyId) =>
        await _db.Set<Budget>()
            .Include(b => b.Account)
            .Include(b => b.CostCenter)
            .Where(b => b.Year == year && b.Month == month && b.CompanyId == companyId)
            .OrderBy(b => b.Account.Code)
            .ToListAsync();

    public async Task<Budget?> GetByIdAsync(Guid id) =>
        await _db.Set<Budget>()
            .Include(b => b.Account)
            .Include(b => b.CostCenter)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task AddAsync(Budget budget) =>
        await _db.Set<Budget>().AddAsync(budget);

    public Task UpdateAsync(Budget budget)
    {
        _db.Set<Budget>().Update(budget);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Budget budget)
    {
        _db.Set<Budget>().Remove(budget);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
