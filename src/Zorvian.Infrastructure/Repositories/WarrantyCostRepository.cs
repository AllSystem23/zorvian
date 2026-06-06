using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class WarrantyCostRepository : IWarrantyCostRepository
{
    private readonly ZorvianDbContext _db;

    public WarrantyCostRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<WarrantyCost>> GetByWarrantyIdAsync(Guid warrantyId) =>
        await _db.Set<WarrantyCost>()
            .Where(c => c.WarrantyId == warrantyId)
            .OrderByDescending(c => c.RegisteredAt)
            .ToListAsync();

    public async Task<List<WarrantyCost>> GetByClaimIdAsync(Guid claimId) =>
        await _db.Set<WarrantyCost>()
            .Where(c => c.ClaimId == claimId)
            .OrderByDescending(c => c.RegisteredAt)
            .ToListAsync();

    public async Task<WarrantyCost?> GetByIdAsync(Guid id) =>
        await _db.Set<WarrantyCost>().FindAsync(id);

    public async Task AddAsync(WarrantyCost cost) =>
        await _db.Set<WarrantyCost>().AddAsync(cost);

    public Task UpdateAsync(WarrantyCost cost)
    {
        _db.Set<WarrantyCost>().Update(cost);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WarrantyCost cost)
    {
        _db.Set<WarrantyCost>().Remove(cost);
        return Task.CompletedTask;
    }

    public async Task<decimal> GetTotalCostByWarrantyAsync(Guid warrantyId) =>
        await _db.Set<WarrantyCost>()
            .Where(c => c.WarrantyId == warrantyId && c.IsBilled)
            .SumAsync(c => c.Quantity * c.UnitCost);

    public async Task<List<(string Category, decimal Total)>> GetCostBreakdownByWarrantyAsync(Guid warrantyId)
    {
        var breakdown = await _db.Set<WarrantyCost>()
            .Where(c => c.WarrantyId == warrantyId && c.IsBilled)
            .GroupBy(c => c.CostCategory)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Quantity * c.UnitCost) })
            .ToListAsync();
        return breakdown.Select(b => (b.Category, b.Total)).ToList();
    }

    public async Task<decimal> GetTotalCostByPeriodAsync(Guid companyId, DateTime from, DateTime to) =>
        await _db.Set<WarrantyCost>()
            .Where(c => c.CompanyId == companyId && c.IsBilled && c.RegisteredAt >= from && c.RegisteredAt <= to)
            .SumAsync(c => c.Quantity * c.UnitCost);

    public async Task<List<(string Category, decimal Total)>> GetCostBreakdownByPeriodAsync(Guid companyId, DateTime from, DateTime to)
    {
        var breakdown = await _db.Set<WarrantyCost>()
            .Where(c => c.CompanyId == companyId && c.IsBilled && c.RegisteredAt >= from && c.RegisteredAt <= to)
            .GroupBy(c => c.CostCategory)
            .Select(g => new { Category = g.Key, Total = g.Sum(c => c.Quantity * c.UnitCost) })
            .ToListAsync();
        return breakdown.Select(b => (b.Category, b.Total)).ToList();
    }

    public async Task<List<(int Year, int Month, decimal Total)>> GetMonthlyCostTrendAsync(Guid companyId, int months)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var trend = await _db.Set<WarrantyCost>()
            .Where(c => c.CompanyId == companyId && c.IsBilled && c.RegisteredAt >= cutoff)
            .GroupBy(c => new { c.RegisteredAt.Year, c.RegisteredAt.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(c => c.Quantity * c.UnitCost) })
            .OrderByDescending(g => g.Year).ThenByDescending(g => g.Month)
            .ToListAsync();
        return trend.Select(t => (t.Year, t.Month, t.Total)).ToList();
    }

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
