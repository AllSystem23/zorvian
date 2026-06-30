using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly ZorvianDbContext _db;

    public SubscriptionPlanRepository(ZorvianDbContext db) => _db = db;

    // ── Plans ──

    public async Task<List<SubscriptionPlan>> GetAllPlansAsync() =>
        await _db.SubscriptionPlans.OrderBy(p => p.PlanId).ToListAsync();

    public async Task<List<SubscriptionPlan>> GetActivePlansAsync() =>
        await _db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.PlanId)
            .ToListAsync();

    public async Task<SubscriptionPlan?> GetPlanByIdAsync(Guid id) =>
        await _db.SubscriptionPlans.FindAsync(id);

    public async Task<SubscriptionPlan?> GetPlanByPlanIdAsync(string planId) =>
        await _db.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.PlanId == planId);

    public async Task<bool> ExistsPlanByPlanIdAsync(string planId, Guid? excludeId = null) =>
        await _db.SubscriptionPlans
            .AnyAsync(p => p.PlanId == planId && p.Id != excludeId);

    public async Task AddPlanAsync(SubscriptionPlan plan) =>
        await _db.SubscriptionPlans.AddAsync(plan);

    public Task UpdatePlanAsync(SubscriptionPlan plan)
    {
        _db.SubscriptionPlans.Update(plan);
        return Task.CompletedTask;
    }

    public Task DeletePlanAsync(SubscriptionPlan plan)
    {
        _db.SubscriptionPlans.Remove(plan);
        return Task.CompletedTask;
    }

    // ── Per-company pricing ──

    public async Task<List<CompanyPlanPricing>> GetAllPricingAsync() =>
        await _db.CompanyPlanPricings
            .Include(p => p.Company)
            .Include(p => p.Plan)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<CompanyPlanPricing>> GetPricingByCompanyAsync(Guid companyId) =>
        await _db.CompanyPlanPricings
            .Include(p => p.Plan)
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<CompanyPlanPricing?> GetPricingByIdAsync(Guid id) =>
        await _db.CompanyPlanPricings
            .Include(p => p.Company)
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<CompanyPlanPricing?> GetActivePricingAsync(Guid companyId, string planId) =>
        await _db.CompanyPlanPricings
            .Include(p => p.Plan)
            .FirstOrDefaultAsync(p =>
                p.CompanyId == companyId &&
                p.PlanId == planId &&
                p.IsActive &&
                p.EffectiveDate <= DateTime.UtcNow &&
                (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow));

    public async Task<bool> HasActivePricingAsync(Guid companyId) =>
        await _db.CompanyPlanPricings
            .AnyAsync(p =>
                p.CompanyId == companyId &&
                p.IsActive &&
                p.EffectiveDate <= DateTime.UtcNow &&
                (p.ExpiryDate == null || p.ExpiryDate > DateTime.UtcNow));

    public async Task AddPricingAsync(CompanyPlanPricing pricing) =>
        await _db.CompanyPlanPricings.AddAsync(pricing);

    public Task UpdatePricingAsync(CompanyPlanPricing pricing)
    {
        _db.CompanyPlanPricings.Update(pricing);
        return Task.CompletedTask;
    }

    public Task DeletePricingAsync(CompanyPlanPricing pricing)
    {
        _db.CompanyPlanPricings.Remove(pricing);
        return Task.CompletedTask;
    }

    public async Task<int> HasCompaniesOnPlanAsync(string planId) =>
        await _db.Companies.CountAsync(c => c.SubscriptionPlan == planId && c.IsActive);

    public async Task<int> SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
