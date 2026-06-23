using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Fleet alert and notification service.
/// Checks fleet conditions (document expiry, license expiry, maintenance overdue,
/// fuel anomalies, high costs) and generates/dispatches alerts.
/// Also handles driver blocking/unblocking.
/// </summary>
public sealed class FleetAlertService
{
    private readonly IFleetDocumentRepository _documentRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IFuelRefillRepository _fuelRepo;
    private readonly IWorkOrderRepository _workOrderRepo;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notification;
    private readonly IMapper _mapper;

    public FleetAlertService(
        IFleetDocumentRepository documentRepo,
        IDriverRepository driverRepo,
        IVehicleRepository vehicleRepo,
        IFuelRefillRepository fuelRepo,
        IWorkOrderRepository workOrderRepo,
        ITenantContext tenant,
        INotificationService notification,
        IMapper mapper)
    {
        _documentRepo = documentRepo;
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
        _fuelRepo = fuelRepo;
        _workOrderRepo = workOrderRepo;
        _tenant = tenant;
        _notification = notification;
        _mapper = mapper;
    }

    // ── Alert CRUD ──

    public async Task<List<FleetAlertResponse>> GetActiveAlertsAsync()
    {
        // Alerts are currently derived from live data (not persisted).
        // This returns alerts computed from current fleet state.
        var alerts = await GenerateAlertsFromFleetStateAsync();
        return alerts.Where(a => a.Status == "Active").ToList();
    }

    public async Task<FleetAlertSummary> GetAlertSummaryAsync()
    {
        var alerts = await GenerateAlertsFromFleetStateAsync();
        var active = alerts.Where(a => a.Status == "Active").ToList();
        return new FleetAlertSummary(
            active.Count,
            active.Count(a => a.Severity == "critical"),
            active.Count(a => a.Severity == "warning"),
            active.Count(a => a.Severity == "info"),
            active.Count(a => a.Status == "Active"),
            active.OrderByDescending(a => a.CreatedAt).Take(20).ToList());
    }

    // ── Driver Blocking ──

    public async Task<DriverBlockResponse?> BlockDriverAsync(Guid driverId, BlockDriverRequest request)
    {
        var driver = await _driverRepo.GetByIdAsync(driverId);
        if (driver == null) return null;

        driver.Status = "Suspended";
        await _driverRepo.UpdateAsync(driver);

        // Generate alert for driver blocking
        await CreateAlertAsync(new CreateFleetAlertRequest(
            "Safety", "warning", "Driver", driverId,
            "Conductor bloqueado",
            $"El conductor {driver.FullName} ha sido bloqueado. Razón: {request.Reason}"));

        return new DriverBlockResponse(
            driver.Id, driver.FullName, true, request.Reason,
            DateTime.UtcNow, _tenant.TenantId.ToString(), null);
    }

    public async Task<DriverBlockResponse?> UnblockDriverAsync(Guid driverId)
    {
        var driver = await _driverRepo.GetByIdAsync(driverId);
        if (driver == null) return null;

        driver.Status = "Active";
        await _driverRepo.UpdateAsync(driver);

        await CreateAlertAsync(new CreateFleetAlertRequest(
            "Safety", "info", "Driver", driverId,
            "Conductor desbloqueado",
            $"El conductor {driver.FullName} ha sido desbloqueado y activado."));

        return new DriverBlockResponse(
            driver.Id, driver.FullName, false, null, null, null, null);
    }

    public async Task<DriverBlockResponse?> GetDriverBlockStatusAsync(Guid driverId)
    {
        var driver = await _driverRepo.GetByIdAsync(driverId);
        if (driver == null) return null;

        var isBlocked = driver.Status == "Suspended";
        return new DriverBlockResponse(
            driver.Id, driver.FullName, isBlocked,
            isBlocked ? "Bloqueado por administrador" : null,
            isBlocked ? driver.UpdatedAt : null,
            isBlocked ? driver.UpdatedBy : null,
            null);
    }

    // ── Alert Generation from Fleet State ──

    private async Task<List<FleetAlertResponse>> GenerateAlertsFromFleetStateAsync()
    {
        var alerts = new List<FleetAlertResponse>();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var companyId = await GetCompanyIdAsync();

        // 1. Document expiry alerts
        alerts.AddRange(await CheckDocumentExpiryAsync(companyId, today));

        // 2. Driver license expiry alerts
        alerts.AddRange(await CheckDriverLicenseExpiryAsync(companyId, today));

        // 3. Maintenance overdue alerts
        alerts.AddRange(await CheckMaintenanceOverdueAsync(companyId, now));

        // 4. Fuel anomaly alerts
        alerts.AddRange(await CheckFuelAnomaliesAsync(companyId));

        // 5. Open work order alerts
        alerts.AddRange(await CheckOpenWorkOrdersAsync(companyId));

        return alerts;
    }

    private async Task<List<FleetAlertResponse>> CheckDocumentExpiryAsync(Guid companyId, DateOnly today)
    {
        var alerts = new List<FleetAlertResponse>();
        var expiringDocs = await _documentRepo.GetExpiringAsync(30, companyId);

        foreach (var doc in expiringDocs)
        {
            var daysUntilExpiry = doc.ExpiryDate.HasValue
                ? doc.ExpiryDate.Value.DayNumber - today.DayNumber
                : int.MaxValue;

            var (severity, title) = daysUntilExpiry <= 0
                ? ("critical", $"Documento vencido: {doc.DocumentType?.Name ?? doc.DocumentNumber}")
                : daysUntilExpiry <= 7
                    ? ("critical", $"Documento vence en {daysUntilExpiry} días: {doc.DocumentType?.Name ?? doc.DocumentNumber}")
                    : ("warning", $"Documento por vencer en {daysUntilExpiry} días: {doc.DocumentType?.Name ?? doc.DocumentNumber}");

            alerts.Add(new FleetAlertResponse(
                Guid.Empty, "Document", severity, doc.EntityType, doc.EntityId,
                title, $"El documento '{doc.DocumentNumber}' del {doc.EntityType} vence el {doc.ExpiryDate}.",
                "Active", false, null, null, null,
                doc.CreatedAt));
        }

        return alerts;
    }

    private async Task<List<FleetAlertResponse>> CheckDriverLicenseExpiryAsync(Guid companyId, DateOnly today)
    {
        var alerts = new List<FleetAlertResponse>();
        var drivers = await _driverRepo.GetAllAsync(companyId);

        foreach (var driver in drivers.Where(d => d.Status == "Active"))
        {
            var daysUntilExpiry = driver.LicenseExpiryDate.DayNumber - today.DayNumber;

            if (daysUntilExpiry <= 30)
            {
                var (severity, title) = daysUntilExpiry <= 0
                    ? ("critical", $"Licencia vencida: {driver.FullName}")
                    : daysUntilExpiry <= 7
                        ? ("critical", $"Licencia vence en {daysUntilExpiry} días: {driver.FullName}")
                        : ("warning", $"Licencia por vencer en {daysUntilExpiry} días: {driver.FullName}");

                alerts.Add(new FleetAlertResponse(
                    Guid.Empty, "License", severity, "Driver", driver.Id,
                    title, $"La licencia {driver.LicenseNumber} de {driver.FullName} vence el {driver.LicenseExpiryDate}.",
                    "Active", false, null, null, null,
                    driver.CreatedAt));
            }
        }

        return alerts;
    }

    private async Task<List<FleetAlertResponse>> CheckMaintenanceOverdueAsync(Guid companyId, DateTime now)
    {
        var alerts = new List<FleetAlertResponse>();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);

        // Check vehicles in maintenance status
        var inMaintenance = vehicles.Where(v => v.Status == "Maintenance").ToList();
        foreach (var vehicle in inMaintenance)
        {
            alerts.Add(new FleetAlertResponse(
                Guid.Empty, "Maintenance", "info", "Vehicle", vehicle.Id,
                $"Vehículo en mantenimiento: {vehicle.Plate}",
                $"El vehículo {vehicle.Brand.Name} {vehicle.Model} ({vehicle.Plate}) está actualmente en mantenimiento.",
                "Active", false, null, null, null,
                vehicle.CreatedAt));
        }

        // Check vehicles out of service
        var outOfService = vehicles.Where(v => v.Status == "OutOfService").ToList();
        foreach (var vehicle in outOfService)
        {
            alerts.Add(new FleetAlertResponse(
                Guid.Empty, "Maintenance", "critical", "Vehicle", vehicle.Id,
                $"Vehículo fuera de servicio: {vehicle.Plate}",
                $"El vehículo {vehicle.Brand.Name} {vehicle.Model} ({vehicle.Plate}) está fuera de servicio.",
                "Active", false, null, null, null,
                vehicle.CreatedAt));
        }

        return alerts;
    }

    private async Task<List<FleetAlertResponse>> CheckFuelAnomaliesAsync(Guid companyId)
    {
        var alerts = new List<FleetAlertResponse>();
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);
        var anomalous = fuelRefills.Where(f => f.AnomalyFlag).ToList();

        foreach (var refill in anomalous)
        {
            alerts.Add(new FleetAlertResponse(
                Guid.Empty, "Fuel", "warning", "Vehicle", refill.VehicleId,
                $"Anomalía de combustible: {refill.Vehicle.Plate}",
                $"Se detectó una anomalía en el abastecimiento del vehículo {refill.Vehicle.Plate} " +
                $"el {refill.RefillDateTime:dd/MM/yyyy}. Litros: {refill.Liters}, Costo: {refill.TotalCost}.",
                "Active", false, null, null, null,
                refill.CreatedAt));
        }

        return alerts;
    }

    private async Task<List<FleetAlertResponse>> CheckOpenWorkOrdersAsync(Guid companyId)
    {
        var alerts = new List<FleetAlertResponse>();
        var workOrders = await _workOrderRepo.GetAllAsync(companyId);
        var openHighPriority = workOrders
            .Where(wo => wo.Status != "Closed" && wo.Status != "Cancelled" &&
                         (wo.Priority == "High" || wo.Priority == "Urgent"))
            .ToList();

        foreach (var wo in openHighPriority)
        {
            var severity = wo.Priority == "Urgent" ? "critical" : "warning";
            alerts.Add(new FleetAlertResponse(
                Guid.Empty, "Maintenance", severity, "WorkOrder", wo.Id,
                $"OT {wo.Priority}: {wo.Number} — {wo.Vehicle.Plate}",
                $"La orden de trabajo {wo.Number} ({wo.Priority}) para el vehículo {wo.Vehicle.Plate} " +
                $"está en estado '{wo.Status}'.",
                "Active", false, null, null, null,
                wo.CreatedAt));
        }

        return alerts;
    }

    // ── Notification Dispatch ──

    public async Task<int> DispatchPendingNotificationsAsync()
    {
        var alerts = await GenerateAlertsFromFleetStateAsync();
        var pending = alerts.Where(a => !a.NotificationSent && a.Status == "Active").ToList();
        var tenantId = _tenant.TenantId.ToString();
        var count = 0;

        foreach (var alert in pending)
        {
            var type = $"fleet_{alert.Category.ToLower()}_{alert.Severity.ToLower()}";
            await _notification.NotifyTenantAsync(
                tenantId, alert.Title, alert.Message, type, alert.EntityId?.ToString());
            count++;
        }

        return count;
    }

    // ── Helpers ──

    public async Task<FleetAlertResponse> CreateAlertAsync(CreateFleetAlertRequest request)
    {
        var alert = new FleetAlertResponse(
            Guid.NewGuid(), request.Category, request.Severity,
            request.EntityType, request.EntityId,
            request.Title, request.Message,
            "Active", false, null, null, null,
            DateTime.UtcNow);

        var tenantId = _tenant.TenantId.ToString();
        var type = $"fleet_{request.Category.ToLower()}_{request.Severity.ToLower()}";
        await _notification.NotifyTenantAsync(
            tenantId, request.Title, request.Message, type, request.EntityId?.ToString());

        return alert;
    }

    private Task<Guid> GetCompanyIdAsync()
    {
        if (_tenant.IsSuperAdmin) return Task.FromResult(Guid.Empty);

        // Extract company ID from tenant context
        if (Guid.TryParse(_tenant.TenantId.ToString(), out var companyId))
            return Task.FromResult(companyId);
        return Task.FromResult(Guid.Empty);
    }
}
