namespace Zorvian.Core.Entities.Fleet;

public sealed class Trip : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public Guid? CoDriverId { get; set; }
    public Driver? CoDriver { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public decimal StartKm { get; set; }
    public decimal? EndKm { get; set; }
    public decimal? TotalKm { get; set; }
    public int? DurationMinutes { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Notes { get; set; }
}
