namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateFuelRefillRequest(
    DateTime RefillDateTime,
    Guid VehicleId,
    Guid DriverId,
    Guid FuelTypeId,
    decimal Liters,
    decimal PricePerLiter,
    decimal TotalCost,
    decimal CurrentKm,
    decimal? HourMeter,
    Guid? SupplierId,
    string RefillType,
    string PaymentMethod,
    string? InvoiceUrl,
    string? Observations,
    bool ValidForCalculation
);

public sealed record UpdateFuelRefillRequest(
    DateTime? RefillDateTime,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? FuelTypeId,
    decimal? Liters,
    decimal? PricePerLiter,
    decimal? TotalCost,
    decimal? CurrentKm,
    decimal? HourMeter,
    Guid? SupplierId,
    string? RefillType,
    string? PaymentMethod,
    string? InvoiceUrl,
    string? Observations,
    bool? ValidForCalculation,
    bool? AnomalyFlag,
    string? AnomalyNotes
);

public sealed record FuelRefillResponse(
    Guid Id,
    DateTime RefillDateTime,
    Guid VehicleId,
    string VehiclePlate,
    string VehicleBrandModel,
    Guid DriverId,
    string DriverName,
    Guid FuelTypeId,
    string FuelTypeName,
    decimal Liters,
    decimal PricePerLiter,
    decimal TotalCost,
    decimal CurrentKm,
    decimal? HourMeter,
    Guid? SupplierId,
    string? SupplierName,
    string RefillType,
    string PaymentMethod,
    string? InvoiceUrl,
    string? Observations,
    bool ValidForCalculation,
    bool AnomalyFlag,
    string? AnomalyNotes,
    DateTime CreatedAt
);
