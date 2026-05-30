namespace Nexora.Application.Interfaces;

public interface INotificationService
{
    Task NotifyUserAsync(string tenantId, string userId, string title, string message, string? type = null, string? relatedEntityId = null);
    Task NotifyTenantAsync(string tenantId, string title, string message, string? type = null, string? relatedEntityId = null);
    Task NotifyApprovalRequiredAsync(string tenantId, Guid vacationId, string approverName);
}
