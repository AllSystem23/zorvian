namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateTripRequest(
    string Code,
    Guid VehicleId,
    Guid DriverId,
    Guid? CoDriverId,
    DateTime StartDateTime,
    DateTime? EndDateTime,
    string Origin,
    string Destination,
    decimal StartKm,
    decimal? EndKm,
    decimal? TotalKm,
    int? DurationMinutes,
    string? Notes
);

public sealed record UpdateTripRequest(
    string? Code,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? CoDriverId,
    DateTime? StartDateTime,
    DateTime? EndDateTime,
    string? Origin,
    string? Destination,
    decimal? StartKm,
    decimal? EndKm,
    decimal? TotalKm,
    int? DurationMinutes,
    string? Status,
    string? Notes
);

public sealed record TripResponse(
    Guid Id,
    string Code,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleBrandModel,
    Guid DriverId,
    string DriverName,
    Guid? CoDriverId,
    string? CoDriverName,
    DateTime StartDateTime,
    DateTime? EndDateTime,
    string Origin,
    string Destination,
    decimal StartKm,
    decimal? EndKm,
    decimal? TotalKm,
    int? DurationMinutes,
    string Status,
    string? Notes,
    DateTime CreatedAt
);
