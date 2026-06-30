using Zorvian.Application.DTOs.SubscriptionPlan;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public sealed class SubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repo;

    public SubscriptionPlanService(ISubscriptionPlanRepository repo) => _repo = repo;

    // ── Plan CRUD ──

    public async Task<List<SubscriptionPlanResponse>> GetAllPlansAsync()
    {
        var plans = await _repo.GetAllPlansAsync();
        return plans.Select(MapPlanResponse).ToList();
    }

    public async Task<List<SubscriptionPlanResponse>> GetActivePlansAsync()
    {
        var plans = await _repo.GetActivePlansAsync();
        return plans.Select(MapPlanResponse).ToList();
    }

    public async Task<SubscriptionPlanResponse?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _repo.GetPlanByIdAsync(id);
        return plan is null ? null : MapPlanResponse(plan);
    }

    public async Task<SubscriptionPlanResponse> CreatePlanAsync(CreateSubscriptionPlanRequest request)
    {
        var planId = request.PlanId.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(planId))
            throw new InvalidOperationException("El ID del plan es requerido.");

        if (await _repo.ExistsPlanByPlanIdAsync(planId))
            throw new InvalidOperationException($"Ya existe un plan con el ID '{planId}'.");

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Name = request.Name,
            Price = request.Price,
            Period = request.Period,
            MaxEmployees = request.MaxEmployees,
            IsPopular = request.IsPopular,
            ShortDescription = request.ShortDescription,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await _repo.AddPlanAsync(plan);
        await _repo.SaveChangesAsync();

        return MapPlanResponse(plan);
    }

    public async Task<SubscriptionPlanResponse?> UpdatePlanAsync(Guid id, UpdateSubscriptionPlanRequest request)
    {
        var plan = await _repo.GetPlanByIdAsync(id);
        if (plan is null) return null;

        if (request.Name is not null) plan.Name = request.Name;
        if (request.Price.HasValue) plan.Price = request.Price.Value;
        if (request.Period is not null) plan.Period = request.Period;
        if (request.MaxEmployees.HasValue) plan.MaxEmployees = request.MaxEmployees.Value;
        if (request.IsPopular.HasValue) plan.IsPopular = request.IsPopular.Value;
        if (request.ShortDescription is not null) plan.ShortDescription = request.ShortDescription;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;
        plan.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdatePlanAsync(plan);
        await _repo.SaveChangesAsync();

        return MapPlanResponse(plan);
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        var plan = await _repo.GetPlanByIdAsync(id);
        if (plan is null) return false;

        // Prevent deletion if companies are actively using this plan
        var companyCount = await _repo.HasCompaniesOnPlanAsync(plan.PlanId);
        if (companyCount > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar el plan '{plan.Name}': {companyCount} empresa(s) activa(s) lo están usando.");

        await _repo.DeletePlanAsync(plan);
        await _repo.SaveChangesAsync();
        return true;
    }

    // ── Per-company pricing ──

    public async Task<List<CompanyPlanPricingResponse>> GetAllPricingAsync()
    {
        var pricing = await _repo.GetAllPricingAsync();
        return pricing.Select(MapPricingResponse).ToList();
    }

    public async Task<List<CompanyPlanPricingResponse>> GetPricingByCompanyAsync(Guid companyId)
    {
        var pricing = await _repo.GetPricingByCompanyAsync(companyId);
        return pricing.Select(MapPricingResponse).ToList();
    }

    public async Task<CompanyPlanPricingResponse?> GetPricingByIdAsync(Guid id)
    {
        var pricing = await _repo.GetPricingByIdAsync(id);
        return pricing is null ? null : MapPricingResponse(pricing);
    }

    public async Task<CompanyPlanPricingResponse> CreatePricingAsync(CreateCompanyPlanPricingRequest request)
    {
        // Validate plan exists
        var plan = await _repo.GetPlanByPlanIdAsync(request.PlanId);
        if (plan is null)
            throw new InvalidOperationException($"No existe un plan con el ID '{request.PlanId}'.");

        // Check for existing active override on same plan
        var existing = await _repo.GetActivePricingAsync(request.CompanyId, request.PlanId);
        if (existing is not null)
            throw new InvalidOperationException(
                $"La empresa ya tiene un precio personalizado activo para el plan '{request.PlanId}'.");

        var pricing = new CompanyPlanPricing
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            PlanId = request.PlanId.Trim().ToLowerInvariant(),
            CustomPrice = request.CustomPrice,
            CustomPeriod = request.CustomPeriod,
            CustomMaxEmployees = request.CustomMaxEmployees,
            EffectiveDate = request.EffectiveDate ?? DateTime.UtcNow,
            ExpiryDate = request.ExpiryDate,
            IsActive = true,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        await _repo.AddPricingAsync(pricing);
        await _repo.SaveChangesAsync();

        return MapPricingResponse(pricing);
    }

    public async Task<CompanyPlanPricingResponse?> UpdatePricingAsync(Guid id, UpdateCompanyPlanPricingRequest request)
    {
        var pricing = await _repo.GetPricingByIdAsync(id);
        if (pricing is null) return null;

        if (request.CustomPrice.HasValue) pricing.CustomPrice = request.CustomPrice;
        if (request.CustomPeriod is not null) pricing.CustomPeriod = request.CustomPeriod;
        if (request.CustomMaxEmployees.HasValue) pricing.CustomMaxEmployees = request.CustomMaxEmployees;
        if (request.ExpiryDate.HasValue) pricing.ExpiryDate = request.ExpiryDate;
        if (request.IsActive.HasValue) pricing.IsActive = request.IsActive.Value;
        if (request.Notes is not null) pricing.Notes = request.Notes;
        pricing.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdatePricingAsync(pricing);
        await _repo.SaveChangesAsync();

        return MapPricingResponse(pricing);
    }

    public async Task<bool> DeletePricingAsync(Guid id)
    {
        var pricing = await _repo.GetPricingByIdAsync(id);
        if (pricing is null) return false;

        await _repo.DeletePricingAsync(pricing);
        await _repo.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Resolves the effective pricing for a company+plan combination.
    /// Merges plan defaults with any active per-company override.
    /// </summary>
    public async Task<ResolvedPlanPricing?> ResolvePricingAsync(Guid companyId, string planId)
    {
        var plan = await _repo.GetPlanByPlanIdAsync(planId);
        if (plan is null) return null;

        var override_ = await _repo.GetActivePricingAsync(companyId, planId);

        return new ResolvedPlanPricing(
            PlanId: plan.PlanId,
            PlanName: plan.Name,
            Price: override_?.CustomPrice ?? plan.Price,
            Period: override_?.CustomPeriod ?? plan.Period,
            MaxEmployees: override_?.CustomMaxEmployees ?? plan.MaxEmployees,
            HasCustomPricing: override_ is not null,
            OverrideId: override_?.Id
        );
    }

    // ── Mapping ──

    private static SubscriptionPlanResponse MapPlanResponse(SubscriptionPlan p) => new(
        p.Id, p.PlanId, p.Name, p.Price, p.Period,
        p.MaxEmployees, p.IsPopular, p.ShortDescription,
        p.IsActive, p.CreatedAt, p.UpdatedAt
    );

    private static CompanyPlanPricingResponse MapPricingResponse(CompanyPlanPricing p) => new(
        p.Id, p.CompanyId, p.Company?.Name,
        p.PlanId, p.Plan?.Name ?? p.PlanId,
        p.CustomPrice, p.CustomPeriod, p.CustomMaxEmployees,
        p.EffectiveDate, p.ExpiryDate, p.IsActive,
        p.Notes, p.CreatedAt
    );
}
