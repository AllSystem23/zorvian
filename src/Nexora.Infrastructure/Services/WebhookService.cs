using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Services;

public sealed class WebhookService : IWebhookService
{
    private readonly NexoraDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookService(NexoraDbContext db, IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
    }

    public async Task PublishAsync<T>(string tenantId, string eventType, T data)
    {
        var subscriptions = await _db.Set<Core.Entities.WebhookSubscription>()
            .Where(s => s.TenantId == tenantId && s.EventType == eventType && s.IsActive)
            .ToListAsync();

        if (subscriptions.Count == 0) return;

        var client = _httpClientFactory.CreateClient("Webhooks");
        var payload = new
        {
            Event = eventType,
            Timestamp = DateTime.UtcNow,
            Data = data
        };

        foreach (var sub in subscriptions)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, sub.TargetUrl)
                {
                    Content = JsonContent.Create(payload)
                };

                // Add signature for security
                var json = System.Text.Json.JsonSerializer.Serialize(payload);
                var signature = GenerateSignature(json, sub.Secret);
                request.Headers.Add("X-Nexora-Signature", signature);

                await client.SendAsync(request);
            }
            catch
            {
                // Log failure or implement retry logic
            }
        }
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
