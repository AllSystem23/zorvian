using AutoMapper;
using Zorvian.Application.DTOs.Goal;
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

    public async Task<GoalDashboardDto> GetDashboardAsync()
    {
        var definitions = await _repo.GetGoalDefinitionsAsync();
        var allAssignments = await _repo.GetGoalAssignmentsAsync();
        var allProgress = await _repo.GetAllProgressAsync();
        var allPayments = await _repo.GetAllIncentivePaymentsAsync();

        var activeGoals = definitions.Count(g => g.Status == "active");

        // Compute per-goal stats
        var goalStats = new List<GoalStatsDto>();
        foreach (var def in definitions)
        {
            var assignments = allAssignments.Where(a => a.GoalDefinitionId == def.Id).ToList();
            var latestProgress = allProgress
                .Where(p => p.GoalAssignment.GoalDefinitionId == def.Id)
                .GroupBy(p => p.GoalAssignmentId)
                .Select(g => g.First())
                .ToList();

            var avgCompliance = latestProgress.Count > 0
                ? latestProgress.Average(p => p.CompliancePercentage)
                : 0m;

            var totalTarget = assignments.Sum(a => a.TargetValue);
            var totalCurrent = latestProgress.Sum(p => p.CurrentValue);

            goalStats.Add(new GoalStatsDto
            {
                GoalId = def.Id,
                GoalName = def.Name,
                GoalType = def.GoalType,
                Status = def.Status,
                Participants = assignments.Count,
                AverageCompliance = Math.Round(avgCompliance, 1),
                TargetValue = totalTarget,
                CurrentValue = totalCurrent,
            });
        }

        // Global compliance: weighted average across all assignments with progress
        var allLatestProgress = allProgress
            .GroupBy(p => p.GoalAssignmentId)
            .Select(g => g.First())
            .ToList();

        var globalCompliance = allLatestProgress.Count > 0
            ? allLatestProgress.Average(p => p.CompliancePercentage)
            : 0m;

        // Incentive budget: sum of all approved incentive payments
        var incentiveBudget = allPayments
            .Where(p => p.Status == "approved")
            .Sum(p => p.FinalAmount);

        // Low performers: assignments below 50% compliance
        var lowPerformers = allLatestProgress
            .Where(p => p.CompliancePercentage < 50 && p.GoalAssignment.Employee is not null)
            .Select(p => new LowPerformerDto
            {
                EmployeeId = p.GoalAssignment.EmployeeId ?? Guid.Empty,
                EmployeeName = $"{p.GoalAssignment.Employee?.FirstName} {p.GoalAssignment.Employee?.LastName}",
                EmployeeCode = p.GoalAssignment.Employee?.EmployeeCode ?? "",
                GoalId = p.GoalAssignment.GoalDefinitionId,
                GoalName = p.GoalAssignment.GoalDefinition?.Name ?? "",
                CompliancePercentage = p.CompliancePercentage,
                TargetValue = p.GoalAssignment.TargetValue,
                CurrentValue = p.CurrentValue,
            })
            .ToList();

        return new GoalDashboardDto
        {
            GlobalCompliance = Math.Round(globalCompliance, 1),
            IncentiveBudget = incentiveBudget,
            TotalGoals = definitions.Count,
            ActiveGoals = activeGoals,
            TotalAssignments = allAssignments.Count,
            GoalStats = goalStats,
            LowPerformers = lowPerformers,
        };
    }

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
