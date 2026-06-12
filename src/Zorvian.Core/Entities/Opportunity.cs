namespace Zorvian.Core.Entities;

public sealed class Opportunity : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }
    
    public Guid StageId { get; set; }
    public PipelineStage Stage { get; set; } = null!;
    
    public decimal EstimatedValue { get; set; }
    public decimal Probability { get; set; }
    public DateOnly? ExpectedClosingDate { get; set; }
    public DateOnly? ActualClosingDate { get; set; }
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = "open"; // Open, Won, Lost, Suspended
    
    public Guid? AssignedToId { get; set; }
    public Employee? AssignedTo { get; set; }
    
    public string? LossReason { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }

    public ICollection<CommercialActivity> Activities { get; set; } = [];
    public ICollection<Quote> Quotes { get; set; } = [];
}
