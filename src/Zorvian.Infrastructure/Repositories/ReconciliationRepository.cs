using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class ReconciliationRepository : IReconciliationRepository
{
    private readonly ZorvianDbContext _db;

    public ReconciliationRepository(ZorvianDbContext db) => _db = db;

    public async Task<Reconciliation?> GetByIdAsync(Guid id) =>
        await _db.Set<Reconciliation>()
            .Include(r => r.BankAccount)
            .Include(r => r.Details)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<Reconciliation>> GetFilteredAsync(Guid? bankAccountId, string? status, DateOnly? dateFrom, DateOnly? dateTo, Guid companyId, int page, int pageSize)
    {
        var query = _db.Set<Reconciliation>()
            .Include(r => r.BankAccount)
            .Where(r => r.CompanyId == companyId);

        if (bankAccountId.HasValue)
            query = query.Where(r => r.BankAccountId == bankAccountId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(r => r.DateFrom >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(r => r.DateTo <= dateTo.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? bankAccountId, string? status, DateOnly? dateFrom, DateOnly? dateTo, Guid companyId)
    {
        var query = _db.Set<Reconciliation>()
            .Where(r => r.CompanyId == companyId);

        if (bankAccountId.HasValue)
            query = query.Where(r => r.BankAccountId == bankAccountId.Value);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(r => r.Status == status);
        if (dateFrom.HasValue)
            query = query.Where(r => r.DateFrom >= dateFrom.Value);
        if (dateTo.HasValue)
            query = query.Where(r => r.DateTo <= dateTo.Value);

        return await query.CountAsync();
    }

    public async Task AddAsync(Reconciliation reconciliation) =>
        await _db.Set<Reconciliation>().AddAsync(reconciliation);

    public Task UpdateAsync(Reconciliation reconciliation)
    {
        _db.Set<Reconciliation>().Update(reconciliation);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Reconciliation reconciliation)
    {
        _db.Set<Reconciliation>().Remove(reconciliation);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();

    public async Task<List<ReconciliationDetail>> GetDetailsByReconciliationIdAsync(Guid reconciliationId) =>
        await _db.Set<ReconciliationDetail>()
            .Where(d => d.ReconciliationId == reconciliationId)
            .OrderBy(d => d.TransactionDate)
            .ToListAsync();

    public async Task AddDetailAsync(ReconciliationDetail detail) =>
        await _db.Set<ReconciliationDetail>().AddAsync(detail);

    public async Task AddDetailsBulkAsync(List<ReconciliationDetail> details) =>
        await _db.Set<ReconciliationDetail>().AddRangeAsync(details);

    public Task ClearDetailsAsync(Guid reconciliationId)
    {
        var details = _db.Set<ReconciliationDetail>().Where(d => d.ReconciliationId == reconciliationId);
        _db.Set<ReconciliationDetail>().RemoveRange(details);
        return Task.CompletedTask;
    }
}
