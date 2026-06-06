namespace Zorvian.Core.Entities;

public sealed class WarrantyAttachment : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid? ClaimId { get; set; }
    public WarrantyClaim? Claim { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long? FileSizeBytes { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? UploadedByEmployeeId { get; set; }
    public Employee? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsPublic { get; set; }
}
