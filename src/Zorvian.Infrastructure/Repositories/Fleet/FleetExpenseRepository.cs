using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories.Fleet;

public sealed class FleetExpenseRepository : IFleetExpenseRepository
{
    private readonly ZorvianDbContext _db;

    public FleetExpenseRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FleetExpense>> GetAllAsync(Guid companyId) =>
        await _db.Set<FleetExpense>()
            .Include(e => e.Category)
            .Include(e => e.Subcategory)
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .Include(e => e.Supplier)
            .Where(e => e.TenantId == companyId.ToString())
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

    public async Task<FleetExpense?> GetByIdAsync(Guid id) =>
        await _db.Set<FleetExpense>()
            .Include(e => e.Category)
            .Include(e => e.Subcategory)
            .Include(e => e.Vehicle)
            .Include(e => e.Driver)
            .Include(e => e.Supplier)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task AddAsync(FleetExpense expense) =>
        await _db.Set<FleetExpense>().AddAsync(expense);

    public Task UpdateAsync(FleetExpense expense)
    {
        _db.Set<FleetExpense>().Update(expense);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(FleetExpense expense)
    {
        _db.Set<FleetExpense>().Remove(expense);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
