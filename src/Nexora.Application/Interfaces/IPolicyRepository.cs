using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface IPolicyRepository
{
    Task AddDocumentAsync(PolicyDocument document);
    Task AddChunksAsync(IEnumerable<PolicyChunk> chunks);
    Task SaveChangesAsync();
}
