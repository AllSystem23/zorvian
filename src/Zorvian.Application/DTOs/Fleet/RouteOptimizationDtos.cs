namespace Zorvian.Application.DTOs.Fleet;

// ── Route Optimization DTOs ──

public sealed record OptimizeRouteRequest(
    Guid RouteId,
    string OptimizationCriteria = "Distance"  // Distance, Time, Cost
);

public sealed record OptimizedRouteResponse(
    Guid RouteId,
    string RouteName,
    decimal TotalDistanceKm,
    int TotalDurationMinutes,
    decimal EstimatedCost,
    List<OptimizedStop> Stops,
    List<string> OptimizationNotes);

public sealed record OptimizedStop(
    int Order,
    string Address,
    Guid? ClientId,
    string? ClientName,
    decimal DistanceFromPreviousKm,
    int EstimatedArrivalMinutes,
    int StopDurationMinutes,
    DateTime? EstimatedArrivalTime,
    DateTime? EstimatedDepartureTime,
    string? Instructions);

// ── Driver Assignment DTOs ──

public sealed record AssignDriverRequest(
    Guid RouteId,
    Guid? PreferredDriverId = null);

public sealed record DriverAssignmentResponse(
    Guid RouteId,
    Guid DriverId,
    string DriverName,
    string LicenseCategory,
    decimal CurrentKmToday,
    int TripsToday,
    string AssignmentReason);

public sealed record AvailableDriverResponse(
    Guid Id,
    string FullName,
    string LicenseCategory,
    string LicenseNumber,
    bool LicenseValid,
    int TripsToday,
    decimal KmToday,
    bool IsAvailable);

// ── Delivery ETA DTOs ──

public sealed record DeliveryEtaRequest(
    Guid RouteId);

public sealed record DeliveryEtaResponse(
    Guid RouteId,
    string RouteName,
    DateTime RouteStartTime,
    List<DeliveryEta> Deliveries);

public sealed record DeliveryEta(
    Guid DeliveryId,
    string DeliveryCode,
    string ClientName,
    string Address,
    int SequenceOrder,
    DateTime EstimatedArrival,
    DateTime EstimatedDeparture,
    decimal DistanceFromPreviousKm,
    int CumulativeMinutes,
    string Status);
