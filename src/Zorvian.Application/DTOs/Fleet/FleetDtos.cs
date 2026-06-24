namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateVehicleBrandRequest(string Name, string? Description);
public sealed record UpdateVehicleBrandRequest(string? Name, string? Description, bool? IsActive);
public sealed record VehicleBrandResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record CreateVehicleTypeRequest(string Name, string? Description);
public sealed record UpdateVehicleTypeRequest(string? Name, string? Description, bool? IsActive);
public sealed record VehicleTypeResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record CreateFuelTypeRequest(string Name, string? Description);
public sealed record UpdateFuelTypeRequest(string? Name, string? Description, bool? IsActive);
public sealed record FuelTypeResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record CreateDriverLicenseCategoryRequest(string Name, string? Description, string CountryCode);
public sealed record UpdateDriverLicenseCategoryRequest(string? Name, string? Description, string? CountryCode, bool? IsActive);
public sealed record DriverLicenseCategoryResponse(Guid Id, string Name, string? Description, string CountryCode, bool IsActive);
