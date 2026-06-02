using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class PolicyService : IPolicyService
{
    private readonly IPolicyRepository _repo;
    private readonly IEmbeddingService _embeddingService;
    private readonly ITenantContext _tenant;

    public PolicyService(IPolicyRepository repo, IEmbeddingService embeddingService, ITenantContext tenant)
    {
        _repo = repo;
        _embeddingService = embeddingService;
        _tenant = tenant;
    }

    public async Task IngestDocumentAsync(string title, string content)
    {
        var document = new PolicyDocument
        {
            Title = title,
            Content = content,
            TenantId = _tenant.TenantId
        };
        await _repo.AddDocumentAsync(document);

        // Split by paragraph
        var chunks = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        var policyChunks = new List<PolicyChunk>();
        foreach (var chunkText in chunks)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkText.Trim());
            policyChunks.Add(new PolicyChunk
            {
                PolicyDocument = document,
                Content = chunkText.Trim(),
                Embedding = embedding,
                TenantId = _tenant.TenantId
            });
        }
        
        await _repo.AddChunksAsync(policyChunks);
        await _repo.SaveChangesAsync();
    }
}
