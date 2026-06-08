using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IApprovalFlowConfigRepository
{
    Task<List<ApprovalFlowConfig>> GetAllAsync(Guid companyId);
    Task<ApprovalFlowConfig?> GetByIdAsync(Guid id);
    Task<ApprovalFlowConfig?> GetByModuleAndEventAsync(string module, string eventType, Guid companyId);
    Task AddAsync(ApprovalFlowConfig config);
    Task UpdateAsync(ApprovalFlowConfig config);
    Task DeleteAsync(ApprovalFlowConfig config);
    Task SaveChangesAsync();
}

public interface IApprovalRequestRepository
{
    Task<List<ApprovalRequest>> GetPendingByRoleAsync(string approverRole, Guid companyId);
    Task<List<ApprovalRequest>> GetByReferenceAsync(Guid referenceId);
    Task<ApprovalRequest?> GetByIdAsync(Guid id);
    Task AddAsync(ApprovalRequest request);
    Task UpdateAsync(ApprovalRequest request);
    Task SaveChangesAsync();
}
