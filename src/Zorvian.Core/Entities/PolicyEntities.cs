namespace Zorvian.Core.Entities;

public sealed class PolicyDocument : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public sealed class PolicyChunk : BaseEntity
{
    public Guid PolicyDocumentId { get; set; }
    public PolicyDocument PolicyDocument { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = []; // This will be mapped to a 'vector' column
}
