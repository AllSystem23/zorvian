namespace Zorvian.Core.Entities.Fleet;

public sealed class Vehicle : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Plate { get; set; } = string.Empty;
    public Guid BrandId { get; set; }
    public VehicleBrand Brand { get; set; } = null!;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Vin { get; set; }
    public string? EngineNumber { get; set; }
    public string? ChassisNumber { get; set; }
    public string? Color { get; set; }
    public Guid VehicleTypeId { get; set; }
    public VehicleType VehicleType { get; set; } = null!;
    public Guid FuelTypeId { get; set; }
    public FuelType FuelType { get; set; } = null!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public decimal CurrentKm { get; set; }
    public decimal PreviousKm { get; set; }
    public decimal? HourMeter { get; set; }
    public decimal LoadCapacityKg { get; set; }
    public decimal? LoadCapacityM3 { get; set; }
    public int? PassengerCapacity { get; set; }
    public string Status { get; set; } = "Active";
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public string? GpsDeviceId { get; set; }
    public Guid? AssetId { get; set; }
    public decimal? PurchaseValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? ImageUrl { get; set; }
}
