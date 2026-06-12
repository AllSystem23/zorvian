using Microsoft.Extensions.Logging;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// CRM Opportunity and Sales Pipeline Service (P3.1)
/// </summary>
public interface ICrmService
{
    Task<Zorvian.Core.Entities.Opportunity> CreateOpportunityAsync(string name, Guid clientId, decimal estimatedValue, string stage, Guid ownerId);
    Task<Zorvian.Core.Entities.Opportunity> UpdateStageAsync(Guid opportunityId, string newStage, string? notes = null);
    Task<List<Zorvian.Core.Entities.Opportunity>> GetOpportunitiesByStageAsync(string stage);
    Task<List<Zorvian.Core.Entities.Opportunity>> GetOpportunitiesByOwnerAsync(Guid ownerId);
    Task<Dictionary<string, int>> GetPipelineStatisticsAsync();
    Task<List<CommercialActivity>> GetOpportunityActivitiesAsync(Guid opportunityId);
}

public class CrmService : ICrmService
{
    private readonly ILogger<CrmService> _logger;
    private readonly IGoalIntegrationService _goalIntegration;

    private static readonly string[] PipelineStages = { "prospecting", "qualified", "proposal", "negotiation", "won", "lost" };
    private static readonly Dictionary<string, int> DefaultProbabilities = new()
    {
        { "prospecting", 10 },
        { "qualified", 25 },
        { "proposal", 50 },
        { "negotiation", 75 },
        { "won", 100 },
        { "lost", 0 }
    };

    public CrmService(ILogger<CrmService> logger, IGoalIntegrationService goalIntegration)
    {
        _logger = logger;
        _goalIntegration = goalIntegration;
    }

    public async Task<Zorvian.Core.Entities.Opportunity> CreateOpportunityAsync(string name, Guid clientId, decimal estimatedValue, string stage, Guid ownerId)
    {
        if (!PipelineStages.Contains(stage))
            throw new ArgumentException($"Invalid stage: {stage}. Valid stages: {string.Join(", ", PipelineStages)}");

        var opportunity = new Zorvian.Core.Entities.Opportunity
        {
            Title = name,
            ClientId = clientId,
            EstimatedValue = estimatedValue,
            Status = stage,
            Probability = DefaultProbabilities.GetValueOrDefault(stage, 10),
            AssignedToId = ownerId,
            ExpectedClosingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))
        };

        _logger.LogInformation("[CRM] Created opportunity {Name} for client {ClientId}", name, clientId);
        
        await _goalIntegration.HandleNewClientAsync(ownerId, clientId);
        
        return opportunity;
    }

    public async Task<Zorvian.Core.Entities.Opportunity> UpdateStageAsync(Guid opportunityId, string newStage, string? notes = null)
    {
        if (!PipelineStages.Contains(newStage))
            throw new ArgumentException($"Invalid stage: {newStage}");

        _logger.LogInformation("[CRM] Opportunity {Id} moved to stage {Stage}", opportunityId, newStage);

        if (newStage == "lost" && string.IsNullOrEmpty(notes))
            throw new ArgumentException("Lost reason is required when marking as lost");

        await Task.CompletedTask;
        return new Zorvian.Core.Entities.Opportunity
        {
            Id = opportunityId,
            Status = newStage,
            Probability = DefaultProbabilities.GetValueOrDefault(newStage, 10),
            LossReason = newStage == "lost" ? notes : null,
            ActualClosingDate = (newStage == "won" || newStage == "lost") ? DateOnly.FromDateTime(DateTime.UtcNow) : null
        };
    }

    public async Task<List<Zorvian.Core.Entities.Opportunity>> GetOpportunitiesByStageAsync(string stage)
    {
        _logger.LogInformation("[CRM] Querying opportunities by stage {Stage}", stage);
        await Task.CompletedTask;
        return new List<Zorvian.Core.Entities.Opportunity>();
    }

    public async Task<List<Zorvian.Core.Entities.Opportunity>> GetOpportunitiesByOwnerAsync(Guid ownerId)
    {
        _logger.LogInformation("[CRM] Querying opportunities by owner {OwnerId}", ownerId);
        await Task.CompletedTask;
        return new List<Zorvian.Core.Entities.Opportunity>();
    }

    public async Task<Dictionary<string, int>> GetPipelineStatisticsAsync()
    {
        var stats = new Dictionary<string, int>();
        foreach (var stage in PipelineStages)
        {
            stats[stage] = 0;
        }
        await Task.CompletedTask;
        return stats;
    }

    public async Task<List<CommercialActivity>> GetOpportunityActivitiesAsync(Guid opportunityId)
    {
        _logger.LogInformation("[CRM] Getting activities for opportunity {Id}", opportunityId);
        await Task.CompletedTask;
        return new List<CommercialActivity>();
    }
}
