namespace Zorvian.Core.Entities.Fleet;

/// <summary>
/// Tracks which geofences a vehicle is currently inside.
/// Used for entry/exit detection when new GPS positions are received.
/// </summary>
public sealed class VehicleGeofenceState : BaseEntity
{
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid GeofenceId { get; set; }
    public Geofence Geofence { get; set; } = null!;

    /// <summary>When the vehicle entered the geofence</summary>
    public DateTime EnteredAt { get; set; }

    /// <summary>When the vehicle last exited the geofence (null if still inside)</summary>
    public DateTime? ExitedAt { get; set; }

    /// <summary>Whether the vehicle is currently inside the geofence</summary>
    public bool IsInside { get; set; } = true;

    /// <summary>Last GPS position that updated this state</summary>
    public Guid? LastPositionId { get; set; }
}
