namespace Zorvian.Core.Entities;

/// <summary>
/// Per-company pricing override for a subscription plan.
/// Allows custom pricing per company (e.g. negotiated rates, discounts).
/// Tenant-scoped via CompanyId.
/// </summary>
public sealed class CompanyPlanPricing : BaseEntity
{
    public string PlanId { get; set; } = string.Empty; // references SubscriptionPlan.PlanId
    public decimal? CustomPrice { get; set; } // null = use plan default
    public string? CustomPeriod { get; set; } // null = use plan default
    public int? CustomMaxEmployees { get; set; } // null = use plan default
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation
    public Company? Company { get; set; }
    public SubscriptionPlan? Plan { get; set; }
}
