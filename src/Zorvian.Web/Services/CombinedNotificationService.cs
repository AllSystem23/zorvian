using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Services;

public sealed class CombinedNotificationService : INotificationService
{
    private readonly ISignalRNotificationService _signalR;
    private readonly IFCMNotificationService _fcm;
    private readonly ITenantContext _tenant;

    public CombinedNotificationService(
        ISignalRNotificationService signalR, 
        IFCMNotificationService fcm, 
        ITenantContext tenant)
    {
        _signalR = signalR;
        _fcm = fcm;
        _tenant = tenant;
    }

    public async Task NotifyUserAsync(string tenantId, string userId, string title, string message, string? type = null, string? relatedEntityId = null)
    {
        await _signalR.NotifyUserAsync(tenantId, userId, title, message, type, relatedEntityId);

        if (Guid.TryParse(userId, out var guid))
            await _fcm.SendToUserAsync(guid, title, message, type, relatedEntityId);
    }

    public async Task NotifyTenantAsync(string tenantId, string title, string message, string? type = null, string? relatedEntityId = null)
    {
        await _signalR.NotifyTenantAsync(tenantId, title, message, type, relatedEntityId);
        await _fcm.SendToTenantAsync(tenantId, title, message, type);
    }

    public async Task NotifyApprovalRequiredAsync(string tenantId, Guid vacationId, string approverName)
    {
        await _signalR.NotifyApprovalRequiredAsync(tenantId, vacationId, approverName);

        var title = "Aprobación requerida";
        var body = $"Nueva solicitud de aprobación de {approverName}";
        await _fcm.SendToTenantAsync(tenantId, title, body, "approval");
    }
}
