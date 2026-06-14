namespace Zorvian.Core.Entities;

public sealed class WebhookDeliveryLog : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public WebhookSubscription? Subscription { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int Attempt { get; set; }
    public int MaxRetries { get; set; }
    public bool Success { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
