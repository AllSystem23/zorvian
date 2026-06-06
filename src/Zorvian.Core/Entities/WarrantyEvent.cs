namespace Zorvian.Core.Entities;

public sealed class WarrantyEvent : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid? ClaimId { get; set; }
    public WarrantyClaim? Claim { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? EventData { get; set; }
    public string? Description { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public bool IsMilestone { get; set; }
}
