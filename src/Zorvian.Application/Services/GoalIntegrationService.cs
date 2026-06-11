using Hangfire;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services.GoalEngine;

namespace Zorvian.Application.Services;

public sealed class GoalIntegrationService : IGoalIntegrationService
{
    private readonly IBackgroundJobClient _jobClient;
    private readonly IGoalRepository _repo;

    public GoalIntegrationService(IBackgroundJobClient jobClient, IGoalRepository repo)
    {
        _jobClient = jobClient;
        _repo = repo;
    }

    public async Task HandleNewSaleAsync(Guid salespersonId, decimal amount)
    {
        await EnqueueProgressUpdate(salespersonId, amount);
    }

    public async Task HandleNewClientAsync(Guid salespersonId, Guid clientId)
    {
        await EnqueueProgressUpdate(salespersonId, 1m);
    }

    public async Task HandleDeliveryAsync(Guid salespersonId, Guid orderId, int quantity)
    {
        await EnqueueProgressUpdate(salespersonId, quantity);
    }

    public async Task HandleCaseSolvedAsync(Guid salespersonId, Guid caseId)
    {
        await EnqueueProgressUpdate(salespersonId, 1m);
    }

    public async Task HandleOrderCompletedAsync(Guid salespersonId, Guid orderId)
    {
        await EnqueueProgressUpdate(salespersonId, 1m);
    }

    private async Task EnqueueProgressUpdate(Guid salespersonId, decimal value)
    {
        var assignments = await _repo.GetGoalAssignmentsByEmployeeIdAsync(salespersonId);
        foreach (var assignment in assignments)
        {
            _jobClient.Enqueue<GoalEngine.GoalEngine>(e => e.ProcessProgressAsync(assignment.Id, value));
        }
    }
}
