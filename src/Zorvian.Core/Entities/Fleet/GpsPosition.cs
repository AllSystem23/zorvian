namespace Zorvian.Core.Entities.Fleet;

public sealed class GpsPosition : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public int? Heading { get; set; }
    public DateTime GpsTimestamp { get; set; }
    public bool? IgnitionOn { get; set; }
    public decimal? Odometer { get; set; }
    public decimal? FuelLevel { get; set; }
    public decimal? Temperature { get; set; }
    public decimal? DeviceBattery { get; set; }
    public int? GsmSignal { get; set; }
    public int? Satellites { get; set; }
}
