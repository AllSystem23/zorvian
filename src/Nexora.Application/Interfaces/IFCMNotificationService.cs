namespace Nexora.Application.Interfaces;

public interface IFCMNotificationService
{
    Task SendToUserAsync(Guid userId, string title, string body, string? type = null, string? relatedEntityId = null);
    Task SendToTenantAsync(string tenantId, string title, string body, string? type = null);
}
