using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class CashRegisterRepository : ICashRegisterRepository
{
    private readonly NexoraDbContext _db;

    public CashRegisterRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<CashRegister?> GetByIdAsync(Guid id) =>
        await _db.Set<CashRegister>()
            .Include(c => c.Employee)
            .Include(c => c.Movements)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<CashRegister?> GetOpenByBranchAsync(Guid branchId) =>
        await _db.Set<CashRegister>()
            .Include(c => c.Employee)
            .Include(c => c.Movements)
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.Status == "open");

    public async Task<List<CashRegister>> GetFilteredAsync(Guid? branchId, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize)
    {
        var query = _db.Set<CashRegister>()
            .Include(c => c.Employee)
            .Include(c => c.Movements)
            .Where(c => c.CompanyId == companyId)
            .AsQueryable();

        if (branchId.HasValue) query = query.Where(c => c.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (fromDate.HasValue) query = query.Where(c => c.OpenedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.OpenedAt <= toDate.Value);

        return await query
            .OrderByDescending(c => c.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? branchId, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId)
    {
        var query = _db.Set<CashRegister>().Where(c => c.CompanyId == companyId).AsQueryable();
        if (branchId.HasValue) query = query.Where(c => c.BranchId == branchId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        if (fromDate.HasValue) query = query.Where(c => c.OpenedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.OpenedAt <= toDate.Value);
        return await query.CountAsync();
    }

    public async Task<decimal> GetTodayIncomeAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<CashMovement>()
            .Where(m => m.BranchId == branchId && m.MovementType == "income" && m.CreatedAt >= today)
            .SumAsync(m => m.Amount);
    }

    public async Task<decimal> GetTodayExpenseAsync(Guid branchId)
    {
        var today = DateTime.UtcNow.Date;
        return await _db.Set<CashMovement>()
            .Where(m => m.BranchId == branchId && m.MovementType == "expense" && m.CreatedAt >= today)
            .SumAsync(m => m.Amount);
    }

    public async Task<int> GetOpenRegistersCountAsync(Guid branchId) =>
        await _db.Set<CashRegister>().CountAsync(c => c.BranchId == branchId && c.Status == "open");

    public async Task AddAsync(CashRegister cashRegister) =>
        await _db.Set<CashRegister>().AddAsync(cashRegister);

    public Task UpdateAsync(CashRegister cashRegister)
    {
        _db.Set<CashRegister>().Update(cashRegister);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
