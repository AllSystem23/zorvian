namespace Zorvian.Core.Entities;

public sealed class WarrantyCommunication : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid? ClaimId { get; set; }
    public WarrantyClaim? Claim { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Direction { get; set; } = "outbound";
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? TemplateId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ExternalId { get; set; }
    public string? Metadata { get; set; }
    public Guid? SentByEmployeeId { get; set; }
    public Employee? SentBy { get; set; }
}
