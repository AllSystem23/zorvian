using AutoMapper;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class GoalService
{
    private readonly IGoalRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;

    public GoalService(IGoalRepository repo, IMapper mapper, ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
    }

    public async Task<List<GoalDefinition>> GetGoalDefinitionsAsync() =>
        await _repo.GetGoalDefinitionsAsync();

    public async Task<GoalDefinition?> GetGoalDefinitionByIdAsync(Guid id) =>
        await _repo.GetGoalDefinitionByIdAsync(id);

    public async Task<GoalDefinition> CreateGoalDefinitionAsync(GoalDefinition definition)
    {
        definition.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddGoalDefinitionAsync(definition);
        return definition;
    }

    public async Task<GoalDefinition?> UpdateGoalDefinitionAsync(Guid id, GoalDefinition definition)
    {
        var existing = await _repo.GetGoalDefinitionByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(definition, existing);
        await _repo.UpdateGoalDefinitionAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteGoalDefinitionAsync(Guid id)
    {
        var existing = await _repo.GetGoalDefinitionByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteGoalDefinitionAsync(id);
        return true;
    }

    public async Task<GoalAssignment> AssignGoalAsync(GoalAssignment assignment)
    {
        assignment.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddGoalAssignmentAsync(assignment);
        return assignment;
    }

    public async Task<List<GoalAssignment>> GetAssignmentsByEmployeeAsync(Guid employeeId) =>
        await _repo.GetGoalAssignmentsByEmployeeIdAsync(employeeId);

    public async Task<List<GoalAssignment>> GetAssignmentsByGoalAsync(Guid goalId)
    {
        var all = await _repo.GetGoalAssignmentsAsync();
        return all.Where(a => a.GoalDefinitionId == goalId).ToList();
    }

    public async Task<GoalProgress> RecordProgressAsync(GoalProgress progress)
    {
        var assignment = await _repo.GetGoalAssignmentByIdAsync(progress.GoalAssignmentId);
        if (assignment is not null && assignment.TargetValue > 0)
        {
            progress.CompliancePercentage = Math.Round((progress.CurrentValue / assignment.TargetValue) * 100, 2);
        }
        progress.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddGoalProgressAsync(progress);
        return progress;
    }

    public async Task<GoalProgress?> EvaluateGoalAsync(Guid assignmentId)
    {
        var assignment = await _repo.GetGoalAssignmentByIdAsync(assignmentId);
        if (assignment is null) return null;

        var progressEntries = await _repo.GetProgressByAssignmentIdAsync(assignmentId);
        var latestProgress = progressEntries.FirstOrDefault();
        if (latestProgress is null) return null;

        if (assignment.TargetValue > 0)
        {
            latestProgress.CompliancePercentage = Math.Round((latestProgress.CurrentValue / assignment.TargetValue) * 100, 2);
        }

        return latestProgress;
    }

    public async Task<List<Incentive>> GetIncentivesAsync() =>
        await _repo.GetIncentivesAsync();

    public async Task<Incentive?> GetIncentiveByIdAsync(Guid id) =>
        await _repo.GetIncentiveByIdAsync(id);

    public async Task<Incentive> CreateIncentiveAsync(Incentive incentive)
    {
        incentive.TenantId = _tenant.TenantId.Value.ToString();
        await _repo.AddIncentiveAsync(incentive);
        return incentive;
    }

    public async Task<Incentive?> UpdateIncentiveAsync(Guid id, Incentive incentive)
    {
        var existing = await _repo.GetIncentiveByIdAsync(id);
        if (existing is null) return null;
        _mapper.Map(incentive, existing);
        await _repo.UpdateIncentiveAsync(existing);
        return existing;
    }

    public async Task<bool> DeleteIncentiveAsync(Guid id)
    {
        var existing = await _repo.GetIncentiveByIdAsync(id);
        if (existing is null) return false;
        await _repo.DeleteIncentiveAsync(id);
        return true;
    }

    public async Task<List<IncentivePayment>> GetIncentivePaymentsAsync(Guid assignmentId) =>
        await _repo.GetIncentivePaymentsByAssignmentIdAsync(assignmentId);

    public async Task<IncentivePayment> ApproveIncentivePaymentAsync(Guid paymentId)
    {
        var payments = await _repo.GetIncentivePaymentsByAssignmentIdAsync(paymentId);
        var payment = payments.FirstOrDefault();
        if (payment is not null)
        {
            payment.Status = "approved";
            payment.PaidAt = DateTime.UtcNow;
        }
        return payment ?? new IncentivePayment();
    }
}
