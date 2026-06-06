namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyCommunicationResponse(
    Guid Id, Guid WarrantyId, Guid? ClaimId,
    string Channel, string Direction,
    string? Subject, string Body,
    string Status, DateTime? SentAt,
    DateTime? DeliveredAt, DateTime? ReadAt,
    string? ErrorMessage
);

public sealed record SendWarrantyCommunicationRequest(
    Guid WarrantyId, Guid? ClaimId,
    string Channel, string? Subject,
    string Body, Guid? TemplateId = null
);
