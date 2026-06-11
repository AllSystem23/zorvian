using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IGoalRepository
{
    Task<List<GoalDefinition>> GetGoalDefinitionsAsync();
    Task<GoalDefinition?> GetGoalDefinitionByIdAsync(Guid id);
    Task AddGoalDefinitionAsync(GoalDefinition definition);
    Task UpdateGoalDefinitionAsync(GoalDefinition definition);
    Task DeleteGoalDefinitionAsync(Guid id);
    Task<List<GoalAssignment>> GetGoalAssignmentsAsync();
    Task<GoalAssignment?> GetGoalAssignmentByIdAsync(Guid id);
    Task<List<GoalAssignment>> GetGoalAssignmentsByEmployeeIdAsync(Guid employeeId);
    Task AddGoalAssignmentAsync(GoalAssignment assignment);
    Task UpdateGoalAssignmentAsync(GoalAssignment assignment);
    Task DeleteGoalAssignmentAsync(Guid id);
    Task<List<GoalProgress>> GetProgressByAssignmentIdAsync(Guid assignmentId);
    Task AddGoalProgressAsync(GoalProgress progress);
    Task<List<Incentive>> GetIncentivesAsync();
    Task<Incentive?> GetIncentiveByIdAsync(Guid id);
    Task AddIncentiveAsync(Incentive incentive);
    Task UpdateIncentiveAsync(Incentive incentive);
    Task DeleteIncentiveAsync(Guid id);
    Task<List<IncentivePayment>> GetIncentivePaymentsByAssignmentIdAsync(Guid assignmentId);
    Task<List<IncentivePayment>> GetIncentivePaymentsByEmployeeIdAsync(Guid employeeId);
    Task AddIncentivePaymentAsync(IncentivePayment payment);
}
