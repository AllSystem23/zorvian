namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateDeliveryRequest(
    string Code,
    Guid? SaleId,
    Guid? ClientId,
    string DeliveryAddress,
    DateOnly ScheduledDate,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    Guid? RouteId,
    Guid? VehicleId,
    Guid? DriverId,
    string? Observations,
    List<CreateDeliveryItemRequest>? Items
);

public sealed record UpdateDeliveryRequest(
    string? Code,
    Guid? SaleId,
    Guid? ClientId,
    string? DeliveryAddress,
    DateOnly? ScheduledDate,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    Guid? RouteId,
    Guid? VehicleId,
    Guid? DriverId,
    string? Status,
    DateTime? DeliveredAt,
    string? ReceiverName,
    string? ReceiverId,
    string? SignatureUrl,
    string? PhotosJson,
    double? GpsLatitude,
    double? GpsLongitude,
    string? Observations,
    string? DocumentUrl
);

public sealed record CreateDeliveryItemRequest(
    Guid ProductId,
    decimal QtyOrdered,
    decimal QtyDelivered,
    decimal QtyReturned,
    string? LotSerial
);

public sealed record DeliveryItemResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal QtyOrdered,
    decimal QtyDelivered,
    decimal QtyReturned,
    string? LotSerial,
    string Status
);

public sealed record DeliveryResponse(
    Guid Id,
    string Code,
    Guid? SaleId,
    Guid? ClientId,
    string? ClientName,
    string DeliveryAddress,
    DateOnly ScheduledDate,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    Guid? RouteId,
    string? RouteName,
    Guid? VehicleId,
    string? VehiclePlate,
    Guid? DriverId,
    string? DriverName,
    string Status,
    DateTime? DeliveredAt,
    string? ReceiverName,
    string? ReceiverId,
    string? SignatureUrl,
    string? Observations,
    string? DocumentUrl,
    List<DeliveryItemResponse> Items,
    DateTime CreatedAt
);
