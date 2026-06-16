namespace Zorvian.Application.DTOs.Fleet;

// ── Delivery Tracking DTOs ──

public sealed record DeliveryStatusUpdateRequest(
    string Status,
    double? GpsLatitude = null,
    double? GpsLongitude = null,
    string? Observations = null);

public sealed record DeliveryTrackingResponse(
    Guid DeliveryId,
    string Code,
    string Status,
    DateTime? DeliveredAt,
    double? GpsLatitude,
    double? GpsLongitude,
    string? Observations,
    DateTime UpdatedAt);

public sealed record DeliveryTrackingTimeline(
    Guid DeliveryId,
    string Code,
    string ClientName,
    string Address,
    string CurrentStatus,
    DateTime ScheduledDate,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    DateTime? DeliveredAt,
    string? VehiclePlate,
    string? DriverName,
    List<TrackingEvent> Events);

public sealed record TrackingEvent(
    string Status,
    DateTime Timestamp,
    double? Latitude,
    double? Longitude,
    string? Notes);

public sealed record ClientDeliveryTracking(
    Guid DeliveryId,
    string Code,
    string Status,
    DateTime? EstimatedArrival,
    DateTime? DeliveredAt,
    string? DriverName,
    string? VehiclePlate,
    string Address,
    DateOnly ScheduledDate,
    List<TrackingEvent> Events);

public sealed record ConfirmDeliveryRequest(
    string ReceiverName,
    string ReceiverId,
    double? GpsLatitude = null,
    double? GpsLongitude = null,
    string? Observations = null,
    string? SignatureUrl = null,
    string? PhotosJson = null);

public sealed record EtaNotificationRequest(
    Guid DeliveryId,
    string NotificationType = "push");  // push, email, both

public sealed record EtaNotificationResponse(
    Guid DeliveryId,
    string Code,
    DateTime EstimatedArrival,
    bool NotificationSent,
    string NotificationType);
