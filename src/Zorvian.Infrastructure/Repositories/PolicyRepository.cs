using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class PolicyRepository : IPolicyRepository
{
    private readonly ZorvianDbContext _db;

    public PolicyRepository(ZorvianDbContext db)
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
