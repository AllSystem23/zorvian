namespace Zorvian.Application.DTOs.Fleet;

public sealed record FleetDashboardData(
    int TotalVehicles,
    int ActiveVehicles,
    int InMaintenance,
    int AvailableDrivers,
    int ActiveRoutes,
    int PendingDeliveries,
    int TripsToday,
    int ExpiringDocuments,
    int OverdueMaintenance,
    List<AlertItem> Alerts
);

public sealed record AlertItem(
    string Type,
    string Severity,
    string Message,
    string? EntityId,
    string? EntityType
);

/// <summary>Raw SQL result: all fleet dashboard counts in a single round-trip.</summary>
public sealed class FleetDashboardScalars
{
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int InMaintenance { get; set; }
    public int AvailableDrivers { get; set; }
    public int ActiveRoutes { get; set; }
    public int PendingDeliveries { get; set; }
    public int TripsToday { get; set; }
    public int ExpiringDocuments { get; set; }
    public int ExpiringSoon { get; set; }
}
