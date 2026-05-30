using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class PolicyRepository : IPolicyRepository
{
    private readonly NexoraDbContext _db;

    public PolicyRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task AddDocumentAsync(PolicyDocument document)
    {
        await _db.PolicyDocuments.AddAsync(document);
    }

    public async Task AddChunksAsync(IEnumerable<PolicyChunk> chunks)
    {
        await _db.PolicyChunks.AddRangeAsync(chunks);
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}
