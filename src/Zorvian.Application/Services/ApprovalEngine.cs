using AutoMapper;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ApprovalEngine : IApprovalEngine
{
    private readonly IApprovalFlowConfigRepository _configRepo;
    private readonly IApprovalRequestRepository _requestRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public ApprovalEngine(
        IApprovalFlowConfigRepository configRepo,
        IApprovalRequestRepository requestRepo,
        ITenantContext tenant,
        IMapper mapper)
    {
        _configRepo = configRepo; _requestRepo = requestRepo;
        _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => _tenant.RequireCompanyId();

    public async Task<ApprovalEvaluationResult> EvaluateAsync(
        string module, string eventType, Guid referenceId, decimal amount, string requestedBy)
    {
        var companyId = CompanyId;
        var config = await _configRepo.GetByModuleAndEventAsync(module, eventType, companyId);
        if (config is null || config.Steps.Count == 0)
            return new ApprovalEvaluationResult(false, null, null);

        var matchingSteps = config.Steps
            .Where(s => (!s.MinAmount.HasValue || amount >= s.MinAmount.Value)
                     && (!s.MaxAmount.HasValue || amount <= s.MaxAmount.Value))
            .OrderBy(s => s.StepOrder)
            .ToList();

        if (matchingSteps.Count == 0)
            return new ApprovalEvaluationResult(false, null, null);

        var request = new ApprovalRequest
        {
            Module = module, EventType = eventType, ReferenceId = referenceId,
            Status = "pending", CurrentStep = 1,
            TotalSteps = matchingSteps.Count,
            RequestedBy = requestedBy, RequestedAt = DateTime.UtcNow,
            CompanyId = companyId,
        };

        await _requestRepo.AddAsync(request);
        await _requestRepo.SaveChangesAsync();

        return new ApprovalEvaluationResult(true, request.Id, "pending");
    }

    public async Task<ApprovalRequestResponse?> ApproveAsync(Guid requestId, string actedBy, string? comment)
    {
        var request = await _requestRepo.GetByIdAsync(requestId);
        if (request is null || request.Status != "pending")
            return null;

        var action = new ApprovalRequestAction
        {
            ApprovalRequestId = requestId, StepOrder = request.CurrentStep,
            Action = "approved", Comment = comment,
            ActedBy = actedBy, ActedAt = DateTime.UtcNow, CompanyId = CompanyId,
        };
        request.Actions.Add(action);

        if (request.CurrentStep >= request.TotalSteps)
        {
            request.Status = "approved";
        }
        else
        {
            request.CurrentStep++;
        }

        await _requestRepo.SaveChangesAsync();
        return _mapper.Map<ApprovalRequestResponse>(request);
    }

    public async Task<ApprovalRequestResponse?> RejectAsync(Guid requestId, string actedBy, string? comment)
    {
        var request = await _requestRepo.GetByIdAsync(requestId);
        if (request is null || request.Status != "pending")
            return null;

        request.Actions.Add(new ApprovalRequestAction
        {
            ApprovalRequestId = requestId, StepOrder = request.CurrentStep,
            Action = "rejected", Comment = comment,
            ActedBy = actedBy, ActedAt = DateTime.UtcNow, CompanyId = CompanyId,
        });
        request.Status = "rejected";

        await _requestRepo.SaveChangesAsync();
        return _mapper.Map<ApprovalRequestResponse>(request);
    }

    public async Task<List<ApprovalRequestResponse>> GetPendingAsync()
    {
        var items = await _requestRepo.GetPendingByRoleAsync("", CompanyId);
        return _mapper.Map<List<ApprovalRequestResponse>>(items);
    }

    public async Task<List<ApprovalRequestResponse>> GetByReferenceAsync(Guid referenceId)
    {
        var items = await _requestRepo.GetByReferenceAsync(referenceId);
        return _mapper.Map<List<ApprovalRequestResponse>>(items);
    }

    public async Task<ApprovalRequestResponse?> GetByIdAsync(Guid id)
    {
        var item = await _requestRepo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<ApprovalRequestResponse>(item);
    }
}
