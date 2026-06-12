namespace Zorvian.Application.DTOs.Commercial;

public record CreateOpportunityRequest(
    string Title,
    string? Description,
    decimal ExpectedValue,
    string CurrencyCode,
    DateTime? ExpectedCloseDate,
    int Probability,
    Guid StageId,
    Guid? LeadId,
    Guid? ClientId,
    string Priority
);

public record UpdateOpportunityRequest(
    string Title,
    string? Description,
    decimal ExpectedValue,
    string CurrencyCode,
    DateTime? ExpectedCloseDate,
    int Probability,
    Guid StageId,
    string Status,
    string Priority,
    string? LostReason
);

public record OpportunityResponse(
    Guid Id,
    string Title,
    string? Description,
    decimal ExpectedValue,
    string CurrencyCode,
    DateTime ExpectedCloseDate,
    int Probability,
    Guid StageId,
    string? StageName,
    string Status,
    string Priority,
    Guid? LeadId,
    Guid? ClientId,
    string? ClientName,
    DateTime CreatedAt
);

public record PipelineStageResponse(
    Guid Id,
    string Name,
    int Order,
    string? Description,
    string Color
);
