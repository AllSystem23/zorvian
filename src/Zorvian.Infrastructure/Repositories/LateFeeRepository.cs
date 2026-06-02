using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class LateFeeRepository : ILateFeeRepository
{
    private readonly ZorvianDbContext _db;

    public LateFeeRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<LateFee?> GetByIdAsync(Guid id) =>
        await _db.Set<LateFee>()
            .Include(lf => lf.CreditInstallment)
            .FirstOrDefaultAsync(lf => lf.Id == id);

    public async Task<List<LateFee>> GetByCreditIdAsync(Guid creditId) =>
        await _db.Set<LateFee>()
            .Where(lf => lf.CreditId == creditId)
            .OrderByDescending(lf => lf.CalculatedAt)
            .ToListAsync();

    public async Task<List<LateFee>> GetByInstallmentIdAsync(Guid installmentId) =>
        await _db.Set<LateFee>()
            .Where(lf => lf.CreditInstallmentId == installmentId)
            .ToListAsync();

    public async Task<List<LateFee>> GetPendingByCreditIdAsync(Guid creditId) =>
        await _db.Set<LateFee>()
            .Where(lf => lf.CreditId == creditId && lf.Status == "pending")
            .ToListAsync();

    public async Task<LateFee?> GetByInstallmentAndDateAsync(Guid installmentId, DateOnly calculatedAt) =>
        await _db.Set<LateFee>()
            .FirstOrDefaultAsync(lf =>
                lf.CreditInstallmentId == installmentId &&
                lf.CalculatedAt == calculatedAt);

    public async Task AddAsync(LateFee lateFee) =>
        await _db.Set<LateFee>().AddAsync(lateFee);

    public Task UpdateAsync(LateFee lateFee)
    {
        _db.Set<LateFee>().Update(lateFee);
        return Task.CompletedTask;
    }

    public async Task AddRangeAsync(IEnumerable<LateFee> lateFees) =>
        await _db.Set<LateFee>().AddRangeAsync(lateFees);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
