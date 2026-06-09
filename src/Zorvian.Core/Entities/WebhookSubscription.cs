namespace Zorvian.Core.Entities;

public sealed class WebhookSubscription : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public int MaxRetries { get; set; } = 3;
    public int RetryIntervalSeconds { get; set; } = 60;
}

