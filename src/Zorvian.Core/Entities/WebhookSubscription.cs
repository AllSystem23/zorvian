using Zorvian.Core.Entities;

namespace Zorvian.Core.Entities;

public sealed class WebhookSubscription : BaseEntity
{
    public string EventType { get; set; } = string.Empty; // e.g., "vacation.approved", "employee.created"
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}
