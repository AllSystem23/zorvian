using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class FCMNotificationService : IFCMNotificationService
{
    private readonly ZorvianDbContext _db;

    public FCMNotificationService(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task SendToUserAsync(Guid userId, string title, string body, string? type = null, string? relatedEntityId = null)
    {
        var tokens = await _db.DeviceTokens
            .Where(d => d.UserId == userId && d.IsActive)
            .Select(d => d.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body,
            },
            Data = new Dictionary<string, string>
            {
                ["type"] = type ?? "general",
                ["relatedEntityId"] = relatedEntityId ?? "",
            },
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
        }
        catch
        {
            // FCM delivery failures are non-critical
        }
    }

    public async Task SendToTenantAsync(string tenantId, string title, string body, string? type = null)
    {
        var tokens = await _db.DeviceTokens
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .Select(d => d.Token)
            .ToListAsync();

        if (tokens.Count == 0) return;

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new FirebaseAdmin.Messaging.Notification
            {
                Title = title,
                Body = body,
            },
            Data = new Dictionary<string, string>
            {
                ["type"] = type ?? "general",
            },
        };

        try
        {
            await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
        }
        catch { }
    }
}
