using Zorvian.Application.DTOs.Approval;

namespace Zorvian.Application.Interfaces;

public interface IApprovalEngine
{
    Task<ApprovalEvaluationResult> EvaluateAsync(string module, string eventType, Guid referenceId, decimal amount, string requestedBy);
    Task<ApprovalRequestResponse?> ApproveAsync(Guid requestId, string actedBy, string? comment);
    Task<ApprovalRequestResponse?> RejectAsync(Guid requestId, string actedBy, string? comment);
    Task<List<ApprovalRequestResponse>> GetPendingAsync();
    Task<List<ApprovalRequestResponse>> GetByReferenceAsync(Guid referenceId);
    Task<ApprovalRequestResponse?> GetByIdAsync(Guid id);
}
