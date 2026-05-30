using Microsoft.AspNetCore.SignalR;
using Nexora.Application.Interfaces;
using Nexora.Web.Hubs;

namespace Nexora.Web.Services;

public sealed class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationService(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyUserAsync(string tenantId, string userId, string title, string message, string? type = null, string? relatedEntityId = null)
    {
        await _hub.Clients.User(userId).SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type,
            relatedEntityId,
            createdAt = DateTime.UtcNow
        });
    }

    public async Task NotifyTenantAsync(string tenantId, string title, string message, string? type = null, string? relatedEntityId = null)
    {
        await _hub.Clients.Group(tenantId).SendAsync("ReceiveNotification", new
        {
            title,
            message,
            type,
            relatedEntityId,
            createdAt = DateTime.UtcNow
        });
    }

    public async Task NotifyApprovalRequiredAsync(string tenantId, Guid vacationId, string approverName)
    {
        await _hub.Clients.Group(tenantId).SendAsync("ReceiveApprovalNotification", new
        {
            vacationId,
            approverName,
            message = $"Nueva solicitud de aprobación de {approverName}",
            createdAt = DateTime.UtcNow
        });
    }
}
