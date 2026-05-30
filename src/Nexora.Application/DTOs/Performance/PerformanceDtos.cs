namespace Nexora.Application.DTOs.Performance;

public sealed record CreateObjectiveRequest(
    string Title,
    string Description,
    Guid EmployeeId,
    DateTime StartDate,
    DateTime EndDate
);

public sealed record CreateKeyResultRequest(
    string Title,
    decimal TargetValue
);

public sealed record UpdateKeyResultRequest(
    decimal CurrentValue
);

public sealed record ObjectiveResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    List<KeyResultResponse> KeyResults
);

public sealed record KeyResultResponse(
    Guid Id,
    string Title,
    decimal TargetValue,
    decimal CurrentValue,
    decimal ProgressPercentage
);
