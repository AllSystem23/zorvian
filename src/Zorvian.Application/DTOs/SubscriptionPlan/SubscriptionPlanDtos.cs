namespace Zorvian.Application.DTOs.SubscriptionPlan;

// ── Plan CRUD DTOs ──

public sealed record CreateSubscriptionPlanRequest(
    string PlanId,
    string Name,
    decimal Price,
    string Period,
    int MaxEmployees,
    bool IsPopular,
    string ShortDescription
);

public sealed record UpdateSubscriptionPlanRequest(
    string? Name,
    decimal? Price,
    string? Period,
    int? MaxEmployees,
    bool? IsPopular,
    string? ShortDescription,
    bool? IsActive
);

public sealed record SubscriptionPlanResponse(
    Guid Id,
    string PlanId,
    string Name,
    decimal Price,
    string Period,
    int MaxEmployees,
    bool IsPopular,
    string ShortDescription,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

// ── Per-Company Pricing DTOs ──

public sealed record CreateCompanyPlanPricingRequest(
    Guid CompanyId,
    string PlanId,
    decimal? CustomPrice,
    string? CustomPeriod,
    int? CustomMaxEmployees,
    DateTime? EffectiveDate,
    DateTime? ExpiryDate,
    string? Notes
);

public sealed record UpdateCompanyPlanPricingRequest(
    decimal? CustomPrice,
    string? CustomPeriod,
    int? CustomMaxEmployees,
    DateTime? ExpiryDate,
    bool? IsActive,
    string? Notes
);

public sealed record CompanyPlanPricingResponse(
    Guid Id,
    Guid CompanyId,
    string? CompanyName,
    string PlanId,
    string? PlanName,
    decimal? CustomPrice,
    string? CustomPeriod,
    int? CustomMaxEmployees,
    DateTime EffectiveDate,
    DateTime? ExpiryDate,
    bool IsActive,
    string? Notes,
    DateTime CreatedAt
);

/// <summary>
/// Resolved pricing for a company — merges plan defaults with any active override.
/// </summary>
public sealed record ResolvedPlanPricing(
    string PlanId,
    string PlanName,
    decimal Price,
    string Period,
    int MaxEmployees,
    bool HasCustomPricing,
    Guid? OverrideId
);
