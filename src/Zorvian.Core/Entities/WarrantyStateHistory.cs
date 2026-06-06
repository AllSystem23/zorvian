namespace Zorvian.Core.Entities;

public sealed class WarrantyStateHistory : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid? ClaimId { get; set; }
    public WarrantyClaim? Claim { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public Guid? ChangedByEmployeeId { get; set; }
    public Employee? ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    public bool SlaBreached { get; set; }
}
