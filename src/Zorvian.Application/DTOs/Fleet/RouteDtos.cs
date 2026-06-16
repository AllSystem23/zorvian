namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateRouteRequest(
    string Code,
    string Name,
    string? Type,
    DateOnly ScheduledDate,
    TimeOnly EstimatedDeparture,
    TimeOnly EstimatedReturn,
    string OriginAddress,
    string? DestinationAddress,
    decimal DistanceEstKm,
    int DurationEstMinutes,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? CoDriverId,
    decimal CostEst,
    string? Notes,
    Guid BranchId,
    List<CreateRoutePointRequest>? Points
);

public sealed record UpdateRouteRequest(
    string? Code,
    string? Name,
    string? Type,
    DateOnly? ScheduledDate,
    TimeOnly? EstimatedDeparture,
    TimeOnly? EstimatedReturn,
    string? OriginAddress,
    string? DestinationAddress,
    decimal? DistanceEstKm,
    int? DurationEstMinutes,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? CoDriverId,
    string? Status,
    decimal? CostEst,
    string? Notes,
    Guid? BranchId
);

public sealed record CreateRoutePointRequest(
    int Order,
    string? Type,
    string Address,
    Guid? ClientId,
    Guid? SaleId,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    int? DurationEstMinutes,
    decimal? DistanceFromPreviousKm,
    string? Instructions
);

public sealed record RoutePointResponse(
    Guid Id,
    int Order,
    string Type,
    string Address,
    Guid? ClientId,
    string? ClientName,
    Guid? SaleId,
    TimeOnly? TimeWindowStart,
    TimeOnly? TimeWindowEnd,
    int? DurationEstMinutes,
    decimal? DistanceFromPreviousKm,
    string? Instructions
);

public sealed record RouteResponse(
    Guid Id,
    string Code,
    string Name,
    string Type,
    DateOnly ScheduledDate,
    TimeOnly EstimatedDeparture,
    TimeOnly EstimatedReturn,
    string OriginAddress,
    string? DestinationAddress,
    decimal DistanceEstKm,
    int DurationEstMinutes,
    Guid? VehicleId,
    string? VehiclePlate,
    Guid? DriverId,
    string? DriverName,
    Guid? CoDriverId,
    string? CoDriverName,
    string Status,
    decimal CostEst,
    string? Notes,
    Guid BranchId,
    string BranchName,
    List<RoutePointResponse> Points,
    DateTime CreatedAt
);
