namespace Nexora.Application.Interfaces;

public interface IWebhookService
{
    Task PublishAsync<T>(string tenantId, string eventType, T data);
}
