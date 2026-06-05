using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CreditRepository : ICreditRepository
{
    private readonly ZorvianDbContext _db;

    public CreditRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<Credit?> GetByIdAsync(Guid id) =>
        await _db.Set<Credit>()
            .Include(c => c.Client)
            .Include(c => c.Employee)
            .Include(c => c.Sale)
            .Include(c => c.Installments.OrderBy(i => i.InstallmentNumber))
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Credit>> GetFilteredAsync(Guid? clientId, string? status, Guid branchId, int page, int pageSize)
    {
        var query = _db.Set<Credit>()
            .Include(c => c.Client)
            .Include(c => c.Installments)
            .AsQueryable();

        if (branchId != Guid.Empty)
            query = query.Where(c => c.BranchId == branchId);

        if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? clientId, string? status, Guid branchId)
    {
        var query = _db.Set<Credit>().AsQueryable();
        if (branchId != Guid.Empty)
            query = query.Where(c => c.BranchId == branchId);
        if (clientId.HasValue) query = query.Where(c => c.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(c => c.Status == status);
        return await query.CountAsync();
    }

    public async Task<string> GenerateCreditNumberAsync(Guid companyId)
    {
        var count = await _db.Set<Credit>().CountAsync(c => c.CompanyId == companyId);
        return $"CRE-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }

    public async Task<int> GetActiveCreditsCountAsync(Guid branchId) =>
        await _db.Set<Credit>().CountAsync(c => c.BranchId == branchId && c.Status == "active");

    public async Task<int> GetOverdueCreditsCountAsync(Guid branchId) =>
        await _db.Set<Credit>().CountAsync(c => c.BranchId == branchId && c.Status == "overdue");

    public async Task<decimal> GetMonthlyRecoveryAsync(Guid branchId)
    {
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _db.Set<CreditPayment>()
            .Where(p => p.BranchId == branchId && p.PaymentDate >= startOfMonth)
            .SumAsync(p => p.Amount);
    }

    public async Task<decimal> GetTotalPortfolioAsync(Guid branchId) =>
        await _db.Set<Credit>()
            .Where(c => c.BranchId == branchId && c.Status == "active")
            .SumAsync(c => c.Balance);

    public async Task<List<CreditInstallment>> GetOverdueInstallmentsAsync(Guid branchId)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _db.Set<CreditInstallment>()
            .Include(ci => ci.Credit)
            .Where(ci => ci.Credit.BranchId == branchId
                && ci.Status == "pending"
                && ci.DueDate < today)
            .OrderBy(ci => ci.DueDate)
            .ToListAsync();
    }

    public async Task<List<CreditInstallment>> GetInstallmentsByCreditIdAsync(Guid creditId) =>
        await _db.Set<CreditInstallment>()
            .Where(ci => ci.CreditId == creditId)
            .OrderBy(ci => ci.InstallmentNumber)
            .ToListAsync();

    public async Task AddAsync(Credit credit) =>
        await _db.Set<Credit>().AddAsync(credit);

    public Task UpdateAsync(Credit credit)
    {
        _db.Set<Credit>().Update(credit);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
