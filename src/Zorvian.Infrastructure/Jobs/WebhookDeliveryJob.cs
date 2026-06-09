using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Jobs;

public sealed class WebhookDeliveryJob
{
    private readonly IServiceScopeFactory _scopeFactory;

    public WebhookDeliveryJob(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(Guid subscriptionId, Guid deliveryLogId, string eventType, object payload, int maxRetries, int retryIntervalSeconds)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var backgroundJobs = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var sub = await db.Set<WebhookSubscription>()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.IsActive);

        if (sub is null) return;

        var log = await db.Set<WebhookDeliveryLog>()
            .FirstOrDefaultAsync(l => l.Id == deliveryLogId);

        if (log is null) return;

        var client = httpClientFactory.CreateClient("Webhooks");
        var wrappedPayload = new
        {
            Event = eventType,
            Timestamp = DateTime.UtcNow,
            Data = payload
        };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl)
            {
                Content = JsonContent.Create(wrappedPayload)
            };

            var json = System.Text.Json.JsonSerializer.Serialize(wrappedPayload);
            var signature = GenerateSignature(json, sub.Secret);
            request.Headers.Add("X-Zorvian-Signature", signature);

            var response = await client.SendAsync(request);

            log.Success = response.IsSuccessStatusCode;
            log.HttpStatusCode = (int)response.StatusCode;
            log.Attempt++;
            log.ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";

            if (!response.IsSuccessStatusCode && log.Attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(retryIntervalSeconds * Math.Pow(2, log.Attempt - 1));
                log.NextRetryAt = DateTime.UtcNow.Add(delay);
                backgroundJobs.Schedule<WebhookDeliveryJob>(
                    j => j.ExecuteAsync(subscriptionId, deliveryLogId, eventType, payload, maxRetries, retryIntervalSeconds),
                    delay);
            }
            else
            {
                log.NextRetryAt = null;
            }
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.Attempt++;
            log.ErrorMessage = ex.Message;

            if (log.Attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(retryIntervalSeconds * Math.Pow(2, log.Attempt - 1));
                log.NextRetryAt = DateTime.UtcNow.Add(delay);
                backgroundJobs.Schedule<WebhookDeliveryJob>(
                    j => j.ExecuteAsync(subscriptionId, deliveryLogId, eventType, payload, maxRetries, retryIntervalSeconds),
                    delay);
            }
            else
            {
                log.NextRetryAt = null;
            }
        }

        log.ExecutedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static string GenerateSignature(string payload, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hash).ToLower();
    }
}
