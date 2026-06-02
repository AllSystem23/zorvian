using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IPolicyRepository
{
    Task AddDocumentAsync(PolicyDocument document);
    Task AddChunksAsync(IEnumerable<PolicyChunk> chunks);
    Task SaveChangesAsync();
}
