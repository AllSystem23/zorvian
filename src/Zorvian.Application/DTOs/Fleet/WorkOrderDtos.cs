namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateWorkOrderRequest(
    string Number,
    Guid VehicleId,
    Guid? DriverId,
    DateTime ReportDateTime,
    Guid? FailureTypeId,
    string? ProblemDescription,
    string? Diagnosis,
    string? RootCause,
    string? SolutionApplied,
    string Priority,
    Guid? WorkshopId,
    string? MechanicResponsible,
    DateTime? StartDate,
    DateTime? EndDate,
    int? DowntimeHours,
    decimal CostEst,
    decimal CostLabor,
    decimal CostParts,
    decimal CostTotal,
    string? DocumentsJson,
    Guid? ApprovedBy
);

public sealed record UpdateWorkOrderRequest(
    string? Number,
    Guid? VehicleId,
    Guid? DriverId,
    DateTime? ReportDateTime,
    Guid? FailureTypeId,
    string? ProblemDescription,
    string? Diagnosis,
    string? RootCause,
    string? SolutionApplied,
    string? Priority,
    string? Status,
    Guid? WorkshopId,
    string? MechanicResponsible,
    DateTime? StartDate,
    DateTime? EndDate,
    int? DowntimeHours,
    decimal? CostEst,
    decimal? CostLabor,
    decimal? CostParts,
    decimal? CostTotal,
    string? DocumentsJson,
    Guid? ApprovedBy
);

public sealed record WorkOrderResponse(
    Guid Id,
    string Number,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleBrandModel,
    Guid? DriverId,
    string? DriverName,
    DateTime ReportDateTime,
    Guid? FailureTypeId,
    string? FailureTypeName,
    string? ProblemDescription,
    string? Diagnosis,
    string? RootCause,
    string? SolutionApplied,
    string Priority,
    string Status,
    Guid? WorkshopId,
    string? WorkshopName,
    string? MechanicResponsible,
    DateTime? StartDate,
    DateTime? EndDate,
    int? DowntimeHours,
    decimal CostEst,
    decimal CostLabor,
    decimal CostParts,
    decimal CostTotal,
    string? DocumentsJson,
    Guid? ApprovedBy,
    DateTime CreatedAt
);

public sealed record CreateMaintenanceScheduleRequest(
    Guid VehicleId,
    Guid? TemplateId,
    string ScheduleType,
    int IntervalValue,
    DateTime? NextExecutionDate,
    decimal? NextExecutionKm,
    decimal? NextExecutionHourMeter,
    DateTime? LastExecutionDate,
    decimal? LastExecutionKm,
    int ToleranceValue
);

public sealed record UpdateMaintenanceScheduleRequest(
    Guid? VehicleId,
    Guid? TemplateId,
    string? ScheduleType,
    int? IntervalValue,
    DateTime? NextExecutionDate,
    decimal? NextExecutionKm,
    decimal? NextExecutionHourMeter,
    DateTime? LastExecutionDate,
    decimal? LastExecutionKm,
    int? ToleranceValue,
    string? Status
);

public sealed record MaintenanceScheduleResponse(
    Guid Id,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleBrandModel,
    Guid? TemplateId,
    string? TemplateName,
    string ScheduleType,
    int IntervalValue,
    DateTime? NextExecutionDate,
    decimal? NextExecutionKm,
    decimal? NextExecutionHourMeter,
    DateTime? LastExecutionDate,
    decimal? LastExecutionKm,
    int ToleranceValue,
    string Status,
    DateTime CreatedAt
);

public sealed record WorkOrderPartResponse(
    Guid Id,
    Guid WorkOrderId,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitCost,
    string? SupplierCode,
    DateTime? WarrantyExpiry
);
