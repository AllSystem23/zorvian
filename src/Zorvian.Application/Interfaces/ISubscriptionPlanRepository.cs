using Zorvian.Application.DTOs.SubscriptionPlan;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ISubscriptionPlanRepository
{
    // Plans
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<List<SubscriptionPlan>> GetActivePlansAsync();
    Task<SubscriptionPlan?> GetPlanByIdAsync(Guid id);
    Task<SubscriptionPlan?> GetPlanByPlanIdAsync(string planId);
    Task<bool> ExistsPlanByPlanIdAsync(string planId, Guid? excludeId = null);
    Task AddPlanAsync(SubscriptionPlan plan);
    Task UpdatePlanAsync(SubscriptionPlan plan);
    Task DeletePlanAsync(SubscriptionPlan plan);

    // Per-company pricing
    Task<List<CompanyPlanPricing>> GetAllPricingAsync();
    Task<List<CompanyPlanPricing>> GetPricingByCompanyAsync(Guid companyId);
    Task<CompanyPlanPricing?> GetPricingByIdAsync(Guid id);
    Task<CompanyPlanPricing?> GetActivePricingAsync(Guid companyId, string planId);
    Task<bool> HasActivePricingAsync(Guid companyId);
    Task<int> HasCompaniesOnPlanAsync(string planId);
    Task AddPricingAsync(CompanyPlanPricing pricing);
    Task UpdatePricingAsync(CompanyPlanPricing pricing);
    Task DeletePricingAsync(CompanyPlanPricing pricing);

    Task<int> SaveChangesAsync();
}
