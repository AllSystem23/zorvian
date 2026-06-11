using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.GoalEngine;
using Zorvian.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace Zorvian.Tests.Services;

public sealed class GoalEngineTests
{
    private readonly Mock<IGoalRepository> _repo = new();
    private readonly Mock<IGoalEvaluator> _evaluator = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly GoalEngine _sut;

    public GoalEngineTests()
    {
        _sut = new GoalEngine(_repo.Object, _evaluator.Object, _cache);
    }

    [Fact]
    public async Task ProcessProgressAsync_EvaluatesAndPersists()
    {
        var assignmentId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var assignment = new GoalAssignment { Id = assignmentId, GoalDefinitionId = goalId, TargetValue = 100 };
        var definition = new GoalDefinition { Id = goalId, Name = "Test Goal" };

        _repo.Setup(r => r.GetGoalAssignmentByIdAsync(assignmentId)).ReturnsAsync(assignment);
        _repo.Setup(r => r.GetGoalDefinitionByIdAsync(goalId)).ReturnsAsync(definition);
        _evaluator.Setup(e => e.EvaluateAsync(assignment, definition, 50m)).ReturnsAsync(new EvaluationResult(50m, true));

        await _sut.ProcessProgressAsync(assignmentId, 50m);

        _repo.Verify(r => r.AddGoalProgressAsync(It.Is<GoalProgress>(p => p.CurrentValue == 50m)), Times.Once);
    }

    [Fact]
    public async Task ProcessProgressAsync_UsesCache()
    {
        var assignmentId = Guid.NewGuid();
        var goalId = Guid.NewGuid();
        var assignment = new GoalAssignment { Id = assignmentId, GoalDefinitionId = goalId };
        var definition = new GoalDefinition { Id = goalId };

        _repo.Setup(r => r.GetGoalAssignmentByIdAsync(assignmentId)).ReturnsAsync(assignment);
        _repo.Setup(r => r.GetGoalDefinitionByIdAsync(goalId)).ReturnsAsync(definition);
        _evaluator.Setup(e => e.EvaluateAsync(It.IsAny<GoalAssignment>(), It.IsAny<GoalDefinition>(), It.IsAny<decimal>()))
            .ReturnsAsync(new EvaluationResult(0, true));

        // First call: Should fetch from repo
        await _sut.ProcessProgressAsync(assignmentId, 10m);
        
        // Second call: Should use cache
        await _sut.ProcessProgressAsync(assignmentId, 20m);

        _repo.Verify(r => r.GetGoalDefinitionByIdAsync(goalId), Times.Once);
    }
}
