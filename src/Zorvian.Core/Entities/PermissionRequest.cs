namespace Zorvian.Core.Entities;

public sealed class PermissionRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalDays { get; set; }
    public decimal BusinessDays { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "pending";
    public string? SupportingDocumentUrl { get; set; }
    public string? SupportingDocumentFileName { get; set; }
    public string? OcrResult { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
