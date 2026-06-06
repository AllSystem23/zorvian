namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyPartRequestResponse(
    Guid Id, Guid WarrantyId, Guid ClaimId,
    Guid ProviderId, Guid ProductId,
    int QuantityRequested, int QuantityReceived,
    decimal? UnitPrice, string RequestNumber,
    DateTime RequestedAt, DateOnly? ExpectedDeliveryDate,
    DateTime? ReceivedAt, string Status,
    string? ProviderAuthorizationCode
);

public sealed record CreateWarrantyPartRequestRequest(
    Guid WarrantyId, Guid ClaimId,
    Guid ProviderId, Guid ProductId,
    int QuantityRequested, decimal? UnitPrice = null,
    DateOnly? ExpectedDeliveryDate = null,
    string? InternalNotes = null
);

public sealed record UpdateWarrantyPartRequestStatusRequest(
    string Status, string? ProviderAuthorizationCode = null,
    string? ProviderNotes = null
);
