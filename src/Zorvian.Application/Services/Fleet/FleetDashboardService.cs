using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetDashboardService
{
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IRouteRepository _routeRepo;
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly ITripRepository _tripRepo;
    private readonly IFleetDocumentRepository _documentRepo;

    public FleetDashboardService(
        IVehicleRepository vehicleRepo,
        IDriverRepository driverRepo,
        IRouteRepository routeRepo,
        IDeliveryRepository deliveryRepo,
        ITripRepository tripRepo,
        IFleetDocumentRepository documentRepo)
    {
        _vehicleRepo = vehicleRepo;
        _driverRepo = driverRepo;
        _routeRepo = routeRepo;
        _deliveryRepo = deliveryRepo;
        _tripRepo = tripRepo;
        _documentRepo = documentRepo;
    }

    public async Task<FleetDashboardData> GetDashboardAsync()
    {
        var vehicles = await _vehicleRepo.GetAllAsync(Guid.Empty);
        var drivers = await _driverRepo.GetAllAsync(Guid.Empty);
        var routes = await _routeRepo.GetAllAsync();
        var deliveries = await _deliveryRepo.GetAllAsync();
        var trips = await _tripRepo.GetAllAsync();
        var documents = await _documentRepo.GetAllAsync(Guid.Empty);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        var totalVehicles = vehicles.Count;
        var activeVehicles = vehicles.Count(v => v.Status == "Active");
        var inMaintenance = vehicles.Count(v => v.Status == "Maintenance");
        var availableDrivers = drivers.Count(d => d.Status == "Available");
        var activeRoutes = routes.Count(r => r.Status == "InProgress" || r.Status == "Planned");
        var pendingDeliveries = deliveries.Count(d => d.Status == "Pending" || d.Status == "InRoute");
        var tripsToday = trips.Count(t => t.StartDateTime.Date == now.Date);
        var expiringDocs = documents.Count(d => d.ExpiryDate != null && d.ExpiryDate.Value <= today && d.Status == "Valid");
        var overdueMaintenance = vehicles.Count(v => v.Status == "Maintenance");

        var alerts = new List<AlertItem>();

        var expiringSoon = documents.Count(d => d.ExpiryDate != null && d.ExpiryDate.Value <= today.AddDays(30) && d.Status == "Valid");
        if (expiringSoon > 0)
            alerts.Add(new AlertItem("Document", "warning", $"{expiringSoon} documento(s) por vencer en 30 días", null, null));

        if (inMaintenance > 0)
            alerts.Add(new AlertItem("Maintenance", "info", $"{inMaintenance} vehículo(s) en mantenimiento", null, null));

        if (pendingDeliveries > 0)
            alerts.Add(new AlertItem("Delivery", "info", $"{pendingDeliveries} entrega(s) pendientes", null, null));

        return new FleetDashboardData(
            totalVehicles, activeVehicles, inMaintenance, availableDrivers,
            activeRoutes, pendingDeliveries, tripsToday, expiringDocs, overdueMaintenance, alerts
        );
    }
}
