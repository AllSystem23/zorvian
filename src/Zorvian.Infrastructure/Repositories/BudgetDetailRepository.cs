using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class BudgetDetailRepository : IBudgetDetailRepository
{
    private readonly ZorvianDbContext _db;

    public BudgetDetailRepository(ZorvianDbContext db) => _db = db;

    public async Task<BudgetDetail?> GetByIdAsync(Guid id) =>
        await _db.Set<BudgetDetail>()
            .Include(bd => bd.Account)
            .Include(bd => bd.CostCenter)
            .FirstOrDefaultAsync(bd => bd.Id == id);

    public async Task<List<BudgetDetail>> GetByBudgetIdAsync(Guid budgetId) =>
        await _db.Set<BudgetDetail>()
            .Include(bd => bd.Account)
            .Include(bd => bd.CostCenter)
            .Where(bd => bd.BudgetId == budgetId)
            .OrderBy(bd => bd.Account.Code)
            .ToListAsync();

    public async Task<List<BudgetDetail>> GetByPeriodAsync(int year, int month, Guid companyId) =>
        await _db.Set<BudgetDetail>()
            .Include(bd => bd.Account)
            .Include(bd => bd.CostCenter)
            .Where(bd => bd.Year == year && bd.Month == month && bd.CompanyId == companyId)
            .OrderBy(bd => bd.Account.Code)
            .ToListAsync();

    public async Task<List<BudgetDetail>> GetFilteredAsync(Guid? budgetId, Guid? accountId, int? year, int? month, Guid companyId)
    {
        var query = _db.Set<BudgetDetail>()
            .Include(bd => bd.Account)
            .Include(bd => bd.CostCenter)
            .Where(bd => bd.CompanyId == companyId);

        if (budgetId.HasValue)
            query = query.Where(bd => bd.BudgetId == budgetId.Value);
        if (accountId.HasValue)
            query = query.Where(bd => bd.AccountId == accountId.Value);
        if (year.HasValue)
            query = query.Where(bd => bd.Year == year.Value);
        if (month.HasValue)
            query = query.Where(bd => bd.Month == month.Value);

        return await query.OrderBy(bd => bd.Year).ThenBy(bd => bd.Month).ThenBy(bd => bd.Account.Code).ToListAsync();
    }

    public async Task AddAsync(BudgetDetail detail) =>
        await _db.Set<BudgetDetail>().AddAsync(detail);

    public Task UpdateAsync(BudgetDetail detail)
    {
        _db.Set<BudgetDetail>().Update(detail);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BudgetDetail detail)
    {
        _db.Set<BudgetDetail>().Remove(detail);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}

public sealed class BudgetTrackingRepository : IBudgetTrackingRepository
{
    private readonly ZorvianDbContext _db;

    public BudgetTrackingRepository(ZorvianDbContext db) => _db = db;

    public async Task<BudgetTracking?> GetByIdAsync(Guid id) =>
        await _db.Set<BudgetTracking>()
            .Include(bt => bt.BudgetDetail)
                .ThenInclude(bd => bd.Account)
            .FirstOrDefaultAsync(bt => bt.Id == id);

    public async Task<List<BudgetTracking>> GetByBudgetDetailIdAsync(Guid budgetDetailId) =>
        await _db.Set<BudgetTracking>()
            .Include(bt => bt.BudgetDetail).ThenInclude(bd => bd.Account)
            .Where(bt => bt.BudgetDetailId == budgetDetailId)
            .OrderByDescending(bt => bt.Year).ThenByDescending(bt => bt.Month)
            .ToListAsync();

    public async Task<List<BudgetTracking>> GetFilteredAsync(Guid? budgetDetailId, int? year, int? month, Guid companyId, int page, int pageSize)
    {
        var query = _db.Set<BudgetTracking>()
            .Include(bt => bt.BudgetDetail).ThenInclude(bd => bd.Account)
            .Where(bt => bt.CompanyId == companyId);

        if (budgetDetailId.HasValue)
            query = query.Where(bt => bt.BudgetDetailId == budgetDetailId.Value);
        if (year.HasValue)
            query = query.Where(bt => bt.Year == year.Value);
        if (month.HasValue)
            query = query.Where(bt => bt.Month == month.Value);

        return await query
            .OrderByDescending(bt => bt.Year).ThenByDescending(bt => bt.Month)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? budgetDetailId, int? year, int? month, Guid companyId)
    {
        var query = _db.Set<BudgetTracking>().Where(bt => bt.CompanyId == companyId);

        if (budgetDetailId.HasValue)
            query = query.Where(bt => bt.BudgetDetailId == budgetDetailId.Value);
        if (year.HasValue)
            query = query.Where(bt => bt.Year == year.Value);
        if (month.HasValue)
            query = query.Where(bt => bt.Month == month.Value);

        return await query.CountAsync();
    }

    public async Task AddAsync(BudgetTracking tracking) =>
        await _db.Set<BudgetTracking>().AddAsync(tracking);

    public Task UpdateAsync(BudgetTracking tracking)
    {
        _db.Set<BudgetTracking>().Update(tracking);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BudgetTracking tracking)
    {
        _db.Set<BudgetTracking>().Remove(tracking);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();

    public async Task<BudgetTracking?> GetByDetailAndPeriodAsync(Guid budgetDetailId, int month, int year) =>
        await _db.Set<BudgetTracking>()
            .FirstOrDefaultAsync(bt => bt.BudgetDetailId == budgetDetailId && bt.Month == month && bt.Year == year);
}
