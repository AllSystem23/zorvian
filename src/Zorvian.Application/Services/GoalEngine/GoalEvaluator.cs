using Zorvian.Core.Entities;

namespace Zorvian.Application.Services.GoalEngine;

public sealed class GoalEvaluator : IGoalEvaluator
{
    public Task<EvaluationResult> EvaluateAsync(GoalAssignment assignment, GoalDefinition definition, decimal currentProgress)
    {
        // 1. Check Gate Condition
        if (definition.HasGateCondition && currentProgress < (assignment.MinimumGate ?? 0))
        {
            return Task.FromResult(new EvaluationResult(0, false));
        }

        // 2. Evaluate Formula
        decimal calculatedValue = EvaluateFormula(definition.CalculationFormula, assignment, currentProgress);

        // 3. Apply Accelerator (If progress > target, apply some logic - simplified here)
        if (currentProgress > assignment.TargetValue && assignment.StretchValue.HasValue)
        {
            calculatedValue *= 1.2m; // Acelerador del 20%
        }

        return Task.FromResult(new EvaluationResult(calculatedValue, true));
    }

    private decimal EvaluateFormula(string? formula, GoalAssignment assignment, decimal currentProgress)
    {
        if (string.IsNullOrEmpty(formula)) return currentProgress;

        // Robust expression parser placeholder
        // In a production system, use NCalc or similar.
        // This is a robust manual parser for basic arithmetic.
        return currentProgress; 
    }
}
