namespace Zorvian.Application.Interfaces;

public interface IWebhookService
{
    Task PublishAsync<T>(string tenantId, string eventType, T data);
}
