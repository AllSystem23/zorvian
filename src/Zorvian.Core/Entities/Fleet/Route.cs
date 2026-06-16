namespace Zorvian.Core.Entities.Fleet;

public sealed class Route : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Urban";
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly EstimatedDeparture { get; set; }
    public TimeOnly EstimatedReturn { get; set; }
    public string OriginAddress { get; set; } = string.Empty;
    public string? DestinationAddress { get; set; }
    public decimal DistanceEstKm { get; set; }
    public int DurationEstMinutes { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid? CoDriverId { get; set; }
    public Driver? CoDriver { get; set; }
    public string Status { get; set; } = "Planned";
    public decimal CostEst { get; set; }
    public string? Notes { get; set; }
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public ICollection<RoutePoint> Points { get; set; } = [];
}
