namespace Zorvian.Application.Config;

/// <summary>
/// Single source of truth for subscription plan definitions.
/// Used by CompanyService for validation/limits and exposed via API for the frontend.
/// Keep this in sync with frontend/lib/features/admin/config/subscription_plans_config.dart
/// </summary>
public sealed record SubscriptionPlanDto(
    string Id,
    string Name,
    string Price,
    string Period,
    int MaxEmployees,
    bool IsPopular,
    string ShortDescription
);

public static class SubscriptionPlanConfig
{
    public static readonly IReadOnlyList<SubscriptionPlanDto> Plans = new[]
    {
        new SubscriptionPlanDto(
            Id: "starter",
            Name: "Starter",
            Price: "Gratis",
            Period: "Para siempre",
            MaxEmployees: 10,
            IsPopular: false,
            ShortDescription: "Hasta 10 empleados, módulos básicos"
        ),
        new SubscriptionPlanDto(
            Id: "professional",
            Name: "Professional",
            Price: "$49",
            Period: "/mes",
            MaxEmployees: 100,
            IsPopular: true,
            ShortDescription: "Hasta 100 empleados, todos los módulos"
        ),
        new SubscriptionPlanDto(
            Id: "enterprise",
            Name: "Enterprise",
            Price: "$199",
            Period: "/mes",
            MaxEmployees: 9999,
            IsPopular: false,
            ShortDescription: "Empleados ilimitados, IA + API"
        ),
    };

    private static readonly IReadOnlyDictionary<string, SubscriptionPlanDto> PlansById =
        Plans.ToDictionary(p => p.Id, p => p);

    /// <summary>
    /// Validates a plan ID and returns the plan config if valid.
    /// </summary>
    public static SubscriptionPlanDto? GetPlan(string planId)
    {
        PlansById.TryGetValue(planId.ToLowerInvariant(), out var plan);
        return plan;
    }

    /// <summary>
    /// Checks if a plan ID is valid.
    /// </summary>
    public static bool IsValidPlan(string planId)
    {
        return PlansById.ContainsKey(planId.ToLowerInvariant());
    }

    /// <summary>
    /// Gets the max employees allowed for a plan, clamped to the plan limit.
    /// </summary>
    public static int ClampMaxEmployees(string planId, int requestedMax)
    {
        var plan = GetPlan(planId);
        if (plan is null) return requestedMax;
        return Math.Min(requestedMax, plan.MaxEmployees);
    }

    /// <summary>
    /// Returns all plan IDs as a set for quick lookup.
    /// </summary>
    public static IReadOnlySet<string> ValidPlanIds { get; } =
        PlansById.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
