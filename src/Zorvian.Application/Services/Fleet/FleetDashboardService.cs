using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetDashboardService
{
    private readonly IVehicleRepository _vehicleRepo;

    public FleetDashboardService(IVehicleRepository vehicleRepo)
    {
        _vehicleRepo = vehicleRepo;
    }

    /// <summary>
    /// All fleet dashboard counts in a single raw SQL round-trip.
    /// Replaces 6 GetAllAsync calls that loaded entire tables into memory.
    /// </summary>
    public async Task<FleetDashboardData> GetDashboardAsync()
    {
        var scalars = await _vehicleRepo.GetDashboardScalarsRawAsync();

        var alerts = new List<AlertItem>();
        if (scalars.ExpiringSoon > 0)
            alerts.Add(new AlertItem("Document", "warning", $"{scalars.ExpiringSoon} documento(s) por vencer en 30 días", null, null));
        if (scalars.InMaintenance > 0)
            alerts.Add(new AlertItem("Maintenance", "info", $"{scalars.InMaintenance} vehículo(s) en mantenimiento", null, null));
        if (scalars.PendingDeliveries > 0)
            alerts.Add(new AlertItem("Delivery", "info", $"{scalars.PendingDeliveries} entrega(s) pendientes", null, null));

        return new FleetDashboardData(
            scalars.TotalVehicles, scalars.ActiveVehicles, scalars.InMaintenance, scalars.AvailableDrivers,
            scalars.ActiveRoutes, scalars.PendingDeliveries, scalars.TripsToday, scalars.ExpiringDocuments,
            scalars.InMaintenance, alerts);
    }
}
