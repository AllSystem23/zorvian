using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Jobs;

namespace Zorvian.Infrastructure.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly ZorvianDbContext _db;
    private readonly IBackgroundJobClient _backgroundJobs;

    public WebhookService(ZorvianDbContext db, IBackgroundJobClient backgroundJobs)
    {
        _db = db;
        _backgroundJobs = backgroundJobs;
    }

    public async Task PublishAsync<T>(string tenantId, string eventType, T data)
    {
        var subscriptions = await _db.Set<WebhookSubscription>()
            .Where(s => s.TenantId == tenantId && s.EventType == eventType && s.IsActive)
            .ToListAsync();

        if (subscriptions.Count == 0) return;

        var payloadJson = JsonSerializer.Serialize(data);

        foreach (var sub in subscriptions)
        {
            var log = new WebhookDeliveryLog
            {
                SubscriptionId = sub.Id,
                EventType = eventType,
                TargetUrl = sub.TargetUrl,
                Attempt = 0,
                MaxRetries = sub.MaxRetries,
                PayloadJson = payloadJson,
                TenantId = sub.TenantId,
            };

            _db.Set<WebhookDeliveryLog>().Add(log);
            await _db.SaveChangesAsync();

            _backgroundJobs.Enqueue<WebhookDeliveryJob>(j =>
                j.ExecuteAsync(sub.Id, log.Id, eventType, data!, sub.MaxRetries, sub.RetryIntervalSeconds));
        }
    }
}
