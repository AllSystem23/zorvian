using Zorvian.Application.Services.GoalEngine;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class GoalEvaluatorTests
{
    private readonly GoalEvaluator _sut = new();

    [Fact]
    public async Task EvaluateAsync_AppliesGateCondition()
    {
        var definition = new GoalDefinition { HasGateCondition = true };
        var assignment = new GoalAssignment { MinimumGate = 100 };

        var result = await _sut.EvaluateAsync(assignment, definition, 50m);

        Assert.Equal(0, result.Value);
        Assert.False(result.PassedGate);
    }

    [Fact]
    public async Task EvaluateAsync_AppliesAccelerator()
    {
        var definition = new GoalDefinition { HasGateCondition = false };
        var assignment = new GoalAssignment { TargetValue = 100, StretchValue = 150 };

        var result = await _sut.EvaluateAsync(assignment, definition, 110m);

        // 110m * 1.2 (accelerator) = 132m
        Assert.Equal(132m, result.Value);
        Assert.True(result.PassedGate);
    }
}
