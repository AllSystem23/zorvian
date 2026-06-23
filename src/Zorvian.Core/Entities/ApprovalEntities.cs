namespace Zorvian.Core.Entities;

public sealed class ApprovalFlowConfig : BaseEntity
{
    public string Module { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<ApprovalFlowStep> Steps { get; set; } = [];
}

public sealed class ApprovalFlowStep : BaseEntity
{
    public Guid ApprovalFlowConfigId { get; set; }
    public ApprovalFlowConfig ApprovalFlowConfig { get; set; } = null!;
    public int StepOrder { get; set; }
    public string ApproverRole { get; set; } = string.Empty;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}

public sealed class ApprovalRequest : BaseEntity
{
    public string Module { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string Status { get; set; } = "pending";
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<ApprovalRequestAction> Actions { get; set; } = [];
}

public sealed class ApprovalRequestAction : BaseEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;
    public int StepOrder { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Comment { get; set; }
    public string ActedBy { get; set; } = string.Empty;
    public DateTime ActedAt { get; set; }
}
