namespace Zorvian.Core.Entities;

public sealed class CommercialActivity : BaseEntity
{
    public string Type { get; set; } = string.Empty; // Call, Meeting, Email, Visit, Task, Note
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Status { get; set; } = "pending"; // Pending, Completed, Cancelled
    
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }
    
    public Guid? OpportunityId { get; set; }
    public Opportunity? Opportunity { get; set; }
    
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    
    public Guid? AssignedToId { get; set; }
    public Employee? AssignedTo { get; set; }
    
    public Guid CompanyId { get; set; }
}
