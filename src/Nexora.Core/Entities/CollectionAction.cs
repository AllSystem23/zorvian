namespace Nexora.Core.Entities;

public sealed class CollectionAction : BaseEntity
{
    public Guid CreditId { get; set; }
    public Credit Credit { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string ActionType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActionDate { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? PromiseAmount { get; set; }
    public DateOnly? PromiseDate { get; set; }
    public string Status { get; set; } = "completed";
    public string? Result { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
