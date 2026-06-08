namespace Zorvian.Application.DTOs.Approval;

public sealed record ApprovalFlowConfigResponse(
    Guid Id, string Module, string EventType, string Description, bool IsActive,
    List<ApprovalFlowStepResponse> Steps
);

public sealed record ApprovalFlowStepResponse(
    Guid Id, int StepOrder, string ApproverRole, decimal? MinAmount, decimal? MaxAmount
);

public sealed record CreateApprovalFlowConfigRequest(
    string Module, string EventType, string Description, List<CreateApprovalFlowStepRequest> Steps
);

public sealed record CreateApprovalFlowStepRequest(
    int StepOrder, string ApproverRole, decimal? MinAmount, decimal? MaxAmount
);

public sealed record UpdateApprovalFlowConfigRequest(
    string? Description, bool? IsActive, List<CreateApprovalFlowStepRequest>? Steps
);

public sealed record ApprovalRequestResponse(
    Guid Id, string Module, string EventType, Guid ReferenceId,
    string Status, int CurrentStep, int TotalSteps,
    string RequestedBy, DateTime RequestedAt, string? Notes,
    List<ApprovalRequestActionResponse> Actions
);

public sealed record ApprovalRequestActionResponse(
    Guid Id, int StepOrder, string Action, string? Comment, string ActedBy, DateTime ActedAt
);

public sealed record ApprovalActionRequest(
    string Comment
);

public sealed record ApprovalEvaluationResult(
    bool RequiresApproval, Guid? ApprovalRequestId, string? Status
);
