namespace Zorvian.Application.DTOs.Commercial;

public sealed record ActivityResponse(
    Guid Id,
    string Type,
    string Subject,
    string? Description,
    string? CreatedByUserName,
    DateTime CreatedAt
);

public sealed record CreateActivityRequest(
    string Type,
    string Subject,
    string? Description
);
