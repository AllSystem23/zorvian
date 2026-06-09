using Microsoft.Extensions.Logging;
using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

/// <summary>
/// CRM Opportunity and Sales Pipeline Service (P3.1)
/// </summary>
public interface ICrmService
{
    Task<Opportunity> CreateOpportunityAsync(string name, Guid clientId, decimal estimatedValue, string stage, Guid ownerId);
    Task<Opportunity> UpdateStageAsync(Guid opportunityId, string newStage, string? notes = null);
    Task<List<Opportunity>> GetOpportunitiesByStageAsync(string stage);
    Task<List<Opportunity>> GetOpportunitiesByOwnerAsync(Guid ownerId);
    Task<Dictionary<string, int>> GetPipelineStatisticsAsync();
    Task<List<Activity>> GetOpportunityActivitiesAsync(Guid opportunityId);
}

public class Opportunity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Stage { get; set; } = "prospecting"; // prospecting, qualified, proposal, negotiation, won, lost
    public int Probability { get; set; } = 10; // 0-100%
    public DateTime ExpectedCloseDate { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public string? LostReason { get; set; }
}

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OpportunityId { get; set; }
    public string Type { get; set; } = string.Empty; // call, email, meeting, note
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public Guid UserId { get; set; }
}

public class CrmService : ICrmService
{
    private readonly ILogger<CrmService> _logger;

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

    public CrmService(ILogger<CrmService> logger)
    {
        _logger = logger;
    }

    public async Task<Opportunity> CreateOpportunityAsync(string name, Guid clientId, decimal estimatedValue, string stage, Guid ownerId)
    {
        if (!PipelineStages.Contains(stage))
            throw new ArgumentException($"Invalid stage: {stage}. Valid stages: {string.Join(", ", PipelineStages)}");

        var opportunity = new Opportunity
        {
            Name = name,
            ClientId = clientId,
            EstimatedValue = estimatedValue,
            Stage = stage,
            Probability = DefaultProbabilities.GetValueOrDefault(stage, 10),
            OwnerId = ownerId,
            ExpectedCloseDate = DateTime.UtcNow.AddDays(30)
        };

        _logger.LogInformation("[CRM] Created opportunity {Name} for client {ClientId}", name, clientId);
        await Task.CompletedTask;
        return opportunity;
    }

    public async Task<Opportunity> UpdateStageAsync(Guid opportunityId, string newStage, string? notes = null)
    {
        if (!PipelineStages.Contains(newStage))
            throw new ArgumentException($"Invalid stage: {newStage}");

        _logger.LogInformation("[CRM] Opportunity {Id} moved to stage {Stage}", opportunityId, newStage);

        if (newStage == "lost" && string.IsNullOrEmpty(notes))
            throw new ArgumentException("Lost reason is required when marking as lost");

        await Task.CompletedTask;
        return new Opportunity
        {
            Id = opportunityId,
            Stage = newStage,
            Probability = DefaultProbabilities.GetValueOrDefault(newStage, 10),
            LostReason = newStage == "lost" ? notes : null,
            ClosedAt = (newStage == "won" || newStage == "lost") ? DateTime.UtcNow : null
        };
    }

    public async Task<List<Opportunity>> GetOpportunitiesByStageAsync(string stage)
    {
        _logger.LogInformation("[CRM] Querying opportunities by stage {Stage}", stage);
        await Task.CompletedTask;
        return new List<Opportunity>();
    }

    public async Task<List<Opportunity>> GetOpportunitiesByOwnerAsync(Guid ownerId)
    {
        _logger.LogInformation("[CRM] Querying opportunities by owner {OwnerId}", ownerId);
        await Task.CompletedTask;
        return new List<Opportunity>();
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

    public async Task<List<Activity>> GetOpportunityActivitiesAsync(Guid opportunityId)
    {
        _logger.LogInformation("[CRM] Getting activities for opportunity {Id}", opportunityId);
        await Task.CompletedTask;
        return new List<Activity>();
    }
}
