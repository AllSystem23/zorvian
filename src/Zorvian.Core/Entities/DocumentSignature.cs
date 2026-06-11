namespace Zorvian.Core.Entities;

public sealed class DocumentSignature : BaseEntity
{
    public Guid DocumentId { get; set; }
    public GeneratedDocument Document { get; set; } = null!;

    public string SignerRole { get; set; } = string.Empty;
    public string SignerType { get; set; } = string.Empty;
    public string SignerId { get; set; } = string.Empty;
    public string SignatureToken { get; set; } = string.Empty;
    public string? IPAddress { get; set; }
    public string Status { get; set; } = "pending"; // pending, signed, rejected
    public DateTime? SignedAt { get; set; }
    public string? SignatureData { get; set; } // PKI Hash or provider reference
    public string? Notes { get; set; }
}
