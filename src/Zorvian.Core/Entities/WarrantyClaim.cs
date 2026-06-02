namespace Zorvian.Core.Entities;

public sealed class WarrantyClaim : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public DateOnly ClaimDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public string? Resolution { get; set; }
    public DateOnly? ResolutionDate { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedBy { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
