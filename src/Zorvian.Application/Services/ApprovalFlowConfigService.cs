using AutoMapper;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class ApprovalFlowConfigService
{
    private readonly IApprovalFlowConfigRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public ApprovalFlowConfigService(IApprovalFlowConfigRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo; _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<List<ApprovalFlowConfigResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync(CompanyId);
        return _mapper.Map<List<ApprovalFlowConfigResponse>>(items);
    }

    public async Task<ApprovalFlowConfigResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<ApprovalFlowConfigResponse>(item);
    }

    public async Task<ApprovalFlowConfigResponse> CreateAsync(CreateApprovalFlowConfigRequest request)
    {
        var entity = new ApprovalFlowConfig
        {
            Module = request.Module, EventType = request.EventType,
            Description = request.Description, IsActive = true, CompanyId = CompanyId,
            Steps = request.Steps.Select(s => new ApprovalFlowStep
            {
                StepOrder = s.StepOrder, ApproverRole = s.ApproverRole,
                MinAmount = s.MinAmount, MaxAmount = s.MaxAmount, CompanyId = CompanyId,
            }).ToList(),
        };
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<ApprovalFlowConfigResponse>(entity);
    }

    public async Task<ApprovalFlowConfigResponse?> UpdateAsync(Guid id, UpdateApprovalFlowConfigRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        if (request.Description != null) entity.Description = request.Description;
        if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;

        if (request.Steps != null)
        {
            entity.Steps.Clear();
            foreach (var s in request.Steps)
            {
                entity.Steps.Add(new ApprovalFlowStep
                {
                    ApprovalFlowConfigId = id, StepOrder = s.StepOrder,
                    ApproverRole = s.ApproverRole, MinAmount = s.MinAmount,
                    MaxAmount = s.MaxAmount, CompanyId = CompanyId,
                });
            }
        }

        await _repo.SaveChangesAsync();
        return _mapper.Map<ApprovalFlowConfigResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }
}
