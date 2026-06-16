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
