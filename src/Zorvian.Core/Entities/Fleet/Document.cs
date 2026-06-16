namespace Zorvian.Core.Entities.Fleet;

public sealed class FleetDocument : BaseEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid DocumentTypeId { get; set; }
    public DocumentType DocumentType { get; set; } = null!;
    public string DocumentNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? FileUrl { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Valid";
    public bool AlertSent { get; set; }
}
