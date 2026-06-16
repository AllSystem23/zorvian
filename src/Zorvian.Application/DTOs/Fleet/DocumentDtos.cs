namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateFleetDocumentRequest(
    string EntityType,
    Guid EntityId,
    Guid DocumentTypeId,
    string DocumentNumber,
    DateOnly IssueDate,
    DateOnly? ExpiryDate,
    string? FileUrl,
    string? Notes
);

public sealed record UpdateFleetDocumentRequest(
    string? DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? FileUrl,
    string? Notes,
    string? Status
);

public sealed record FleetDocumentResponse(
    Guid Id,
    string EntityType,
    Guid EntityId,
    Guid DocumentTypeId,
    string DocumentTypeName,
    bool DocumentTypeHasExpiry,
    string DocumentNumber,
    DateOnly IssueDate,
    DateOnly? ExpiryDate,
    string? FileUrl,
    string? Notes,
    string Status,
    bool AlertSent,
    DateTime CreatedAt
);
