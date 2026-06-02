namespace Zorvian.Application.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}

public interface IChatService
{
    Task<string> ChatAsync(string tenantId, string question);
}
