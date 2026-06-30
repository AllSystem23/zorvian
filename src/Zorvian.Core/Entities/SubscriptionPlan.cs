namespace Zorvian.Core.Entities;

/// <summary>
/// Global subscription plan catalog. Not tenant-scoped — shared across all companies.
/// Managed by SuperAdmin via CRUD endpoints.
/// </summary>
public sealed class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string PlanId { get; set; } = string.Empty; // e.g. "starter", "professional", "enterprise"
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Period { get; set; } = "/mes";
    public int MaxEmployees { get; set; }
    public bool IsPopular { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
