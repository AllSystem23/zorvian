namespace Zorvian.Core.Entities;

public sealed class ApprovalFlow : BaseEntity
{
    public string RequestType { get; set; } = string.Empty;
    public Guid RequestId { get; set; }
    public VacationRequest Request { get; set; } = null!;
    public int Step { get; set; }
    public Guid? ApproverId { get; set; }
    public Employee? Approver { get; set; }
    public string Status { get; set; } = "pending";
    public string? Comments { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
