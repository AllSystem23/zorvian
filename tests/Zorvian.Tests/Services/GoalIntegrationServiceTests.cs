using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Application.Services.GoalEngine;
using Zorvian.Core.Entities;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Zorvian.Tests.Services;

public sealed class GoalIntegrationServiceTests
{
    private readonly Mock<IBackgroundJobClient> _jobClient = new();
    private readonly Mock<IGoalRepository> _repo = new();
    private readonly GoalIntegrationService _sut;

    public GoalIntegrationServiceTests()
    {
        _sut = new GoalIntegrationService(_jobClient.Object, _repo.Object);
    }

    [Fact]
    public async Task HandleNewSaleAsync_EnqueuesJobs()
    {
        var salespersonId = Guid.NewGuid();
        var assignments = new List<GoalAssignment>
        {
            new() { Id = Guid.NewGuid() },
            new() { Id = Guid.NewGuid() }
        };

        _repo.Setup(r => r.GetGoalAssignmentsByEmployeeIdAsync(salespersonId)).ReturnsAsync(assignments);

        await _sut.HandleNewSaleAsync(salespersonId, 1000m);

        _jobClient.Verify(j => j.Create(
            It.IsAny<Job>(),
            It.IsAny<EnqueuedState>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleNewClientAsync_EnqueuesJobs()
    {
        var salespersonId = Guid.NewGuid();
        var assignments = new List<GoalAssignment> { new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.GetGoalAssignmentsByEmployeeIdAsync(salespersonId)).ReturnsAsync(assignments);

        await _sut.HandleNewClientAsync(salespersonId, Guid.NewGuid());

        _jobClient.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task HandleDeliveryAsync_EnqueuesJobs()
    {
        var salespersonId = Guid.NewGuid();
        var assignments = new List<GoalAssignment> { new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.GetGoalAssignmentsByEmployeeIdAsync(salespersonId)).ReturnsAsync(assignments);

        await _sut.HandleDeliveryAsync(salespersonId, Guid.NewGuid(), 5);

        _jobClient.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task HandleCaseSolvedAsync_EnqueuesJobs()
    {
        var salespersonId = Guid.NewGuid();
        var assignments = new List<GoalAssignment> { new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.GetGoalAssignmentsByEmployeeIdAsync(salespersonId)).ReturnsAsync(assignments);

        await _sut.HandleCaseSolvedAsync(salespersonId, Guid.NewGuid());

        _jobClient.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()), Times.Once);
    }

    [Fact]
    public async Task HandleOrderCompletedAsync_EnqueuesJobs()
    {
        var salespersonId = Guid.NewGuid();
        var assignments = new List<GoalAssignment> { new() { Id = Guid.NewGuid() } };
        _repo.Setup(r => r.GetGoalAssignmentsByEmployeeIdAsync(salespersonId)).ReturnsAsync(assignments);

        await _sut.HandleOrderCompletedAsync(salespersonId, Guid.NewGuid());

        _jobClient.Verify(j => j.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()), Times.Once);
    }
}
