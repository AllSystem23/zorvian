namespace Zorvian.Core.Entities.Fleet;

public sealed class Geofence : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Circle";
    public string CoordinatesJson { get; set; } = string.Empty;
    public double? Radius { get; set; }
    public bool Active { get; set; } = true;
}
