namespace Zorvian.Application.DTOs.Fleet;

// ── GPS Position DTOs ──

public sealed record GpsPositionResponse(
    Guid Id,
    Guid VehicleId,
    string VehiclePlate,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Speed,
    int? Heading,
    DateTime GpsTimestamp,
    bool? IgnitionOn,
    decimal? Odometer,
    decimal? FuelLevel,
    decimal? Temperature,
    decimal? DeviceBattery,
    int? GsmSignal,
    int? Satellites);

public sealed record ReceiveGpsPositionRequest(
    string DeviceId,
    Guid? VehicleId,
    double Latitude,
    double Longitude,
    double? Altitude,
    double? Speed,
    int? Heading,
    DateTime GpsTimestamp,
    bool? IgnitionOn,
    decimal? Odometer,
    decimal? FuelLevel,
    decimal? Temperature,
    decimal? DeviceBattery,
    int? GsmSignal,
    int? Satellites);

public sealed record BulkReceiveGpsRequest(
    List<ReceiveGpsPositionRequest> Positions);

public sealed record VehiclePositionSummary(
    Guid VehicleId,
    string VehiclePlate,
    string VehicleBrandModel,
    double Latitude,
    double Longitude,
    double? Speed,
    int? Heading,
    DateTime LastUpdate,
    bool? IgnitionOn,
    decimal? FuelLevel,
    string? DriverName);

public sealed record GpsHistoryResponse(
    Guid VehicleId,
    string VehiclePlate,
    List<GpsPositionResponse> Positions);

// ── Geofence DTOs ──

public sealed record GeofenceResponse(
    Guid Id,
    string Name,
    string Type,
    string CoordinatesJson,
    double? Radius,
    bool Active,
    DateTime CreatedAt);

public sealed record CreateGeofenceRequest(
    string Name,
    string Type = "Circle",
    string CoordinatesJson = "[]",
    double? Radius = null);

public sealed record UpdateGeofenceRequest(
    string? Name = null,
    string? Type = null,
    string? CoordinatesJson = null,
    double? Radius = null,
    bool? Active = null);

public sealed record GeofenceCheckRequest(
    double Latitude,
    double Longitude);

public sealed record GeofenceCheckResponse(
    bool IsInside,
    string? GeofenceName,
    Guid? GeofenceId);

