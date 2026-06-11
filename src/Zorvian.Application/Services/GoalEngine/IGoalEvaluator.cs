using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.GoalEngine;

public record EvaluationResult(decimal Value, bool PassedGate);

public interface IGoalEvaluator
{
    Task<EvaluationResult> EvaluateAsync(GoalAssignment assignment, GoalDefinition definition, decimal currentProgress);
}
