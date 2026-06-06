namespace Zorvian.Application.DTOs.Warranty;

public sealed record AssignWorkshopRequest(
    Guid WorkshopId,
    Guid? TechnicianId,
    int? SlaHoursOverride = null,
    string? Notes = null
);

public sealed record ReferToProviderRequest(
    Guid ProviderId,
    string Reason,
    string? AuthorizationCode = null
);

public sealed record UpdateClaimPriorityRequest(
    string Priority
);

public sealed record ProcessReplacementRequest(
    string ProviderAuthorizationCode,
    Guid NewProductId,
    string NewSerialNumber,
    string? Notes
);
