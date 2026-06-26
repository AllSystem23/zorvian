using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class GoalRepository : IGoalRepository
{
    private readonly ZorvianDbContext _db;

    public GoalRepository(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<List<GoalDefinition>> GetGoalDefinitionsAsync() =>
        await _db.GoalDefinitions
            .OrderBy(g => g.Name)
            .ToListAsync();

    public async Task<GoalDefinition?> GetGoalDefinitionByIdAsync(Guid id) =>
        await _db.GoalDefinitions
            .Include(g => g.Assignments)
            .FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddGoalDefinitionAsync(GoalDefinition definition) =>
        await _db.GoalDefinitions.AddAsync(definition);

    public Task UpdateGoalDefinitionAsync(GoalDefinition definition)
    {
        _db.GoalDefinitions.Update(definition);
        return Task.CompletedTask;
    }

    public async Task DeleteGoalDefinitionAsync(Guid id)
    {
        var goal = await _db.GoalDefinitions.FindAsync(id);
        if (goal is not null)
            _db.GoalDefinitions.Remove(goal);
    }

    public async Task<List<GoalAssignment>> GetGoalAssignmentsAsync() =>
        await _db.GoalAssignments
            .Include(a => a.GoalDefinition)
            .Include(a => a.Employee)
            .ToListAsync();

    public async Task<GoalAssignment?> GetGoalAssignmentByIdAsync(Guid id) =>
        await _db.GoalAssignments
            .Include(a => a.GoalDefinition)
            .Include(a => a.Employee)
            .Include(a => a.ProgressEntries)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<GoalAssignment>> GetGoalAssignmentsByEmployeeIdAsync(Guid employeeId) =>
        await _db.GoalAssignments
            .Include(a => a.GoalDefinition)
            .Where(a => a.EmployeeId == employeeId)
            .ToListAsync();

    public async Task AddGoalAssignmentAsync(GoalAssignment assignment) =>
        await _db.GoalAssignments.AddAsync(assignment);

    public Task UpdateGoalAssignmentAsync(GoalAssignment assignment)
    {
        _db.GoalAssignments.Update(assignment);
        return Task.CompletedTask;
    }

    public async Task DeleteGoalAssignmentAsync(Guid id)
    {
        var assignment = await _db.GoalAssignments.FindAsync(id);
        if (assignment is not null)
            _db.GoalAssignments.Remove(assignment);
    }

    public async Task<List<GoalProgress>> GetProgressByAssignmentIdAsync(Guid assignmentId) =>
        await _db.GoalProgressEntries
            .Where(p => p.GoalAssignmentId == assignmentId)
            .OrderByDescending(p => p.EvaluationDate)
            .ToListAsync();

    public async Task AddGoalProgressAsync(GoalProgress progress) =>
        await _db.GoalProgressEntries.AddAsync(progress);

    public async Task<List<Incentive>> GetIncentivesAsync() =>
        await _db.Incentives
            .OrderBy(i => i.Name)
            .ToListAsync();

    public async Task<Incentive?> GetIncentiveByIdAsync(Guid id) =>
        await _db.Incentives.FindAsync(id);

    public async Task AddIncentiveAsync(Incentive incentive) =>
        await _db.Incentives.AddAsync(incentive);

    public Task UpdateIncentiveAsync(Incentive incentive)
    {
        _db.Incentives.Update(incentive);
        return Task.CompletedTask;
    }

    public async Task DeleteIncentiveAsync(Guid id)
    {
        var incentive = await _db.Incentives.FindAsync(id);
        if (incentive is not null)
            _db.Incentives.Remove(incentive);
    }

    public async Task<List<IncentivePayment>> GetIncentivePaymentsByAssignmentIdAsync(Guid assignmentId) =>
        await _db.IncentivePayments
            .Include(p => p.Incentive)
            .Include(p => p.Employee)
            .Where(p => p.GoalAssignmentId == assignmentId)
            .ToListAsync();

    public async Task<List<IncentivePayment>> GetIncentivePaymentsByEmployeeIdAsync(Guid employeeId) =>
        await _db.IncentivePayments
            .Include(p => p.Incentive)
            .Include(p => p.GoalAssignment)
            .Where(p => p.EmployeeId == employeeId)
            .ToListAsync();

    public async Task AddIncentivePaymentAsync(IncentivePayment payment) =>
        await _db.IncentivePayments.AddAsync(payment);

    public async Task<List<GoalProgress>> GetAllProgressAsync() =>
        await _db.GoalProgressEntries
            .Include(p => p.GoalAssignment)
                .ThenInclude(a => a.GoalDefinition)
            .Include(p => p.GoalAssignment)
                .ThenInclude(a => a.Employee)
            .OrderByDescending(p => p.EvaluationDate)
            .ToListAsync();

    public async Task<List<IncentivePayment>> GetAllIncentivePaymentsAsync() =>
        await _db.IncentivePayments
            .Include(p => p.Incentive)
            .Include(p => p.GoalAssignment)
                .ThenInclude(a => a.GoalDefinition)
            .Include(p => p.Employee)
            .ToListAsync();
}
