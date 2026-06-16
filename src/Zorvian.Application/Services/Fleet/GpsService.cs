using System.Text.Json;
using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// GPS tracking service: receive positions from devices, query history,
/// compute latest per vehicle, check geofence containment,
/// and generate entry/exit alerts with notifications.
/// </summary>
public sealed class GpsService
{
    private readonly IGpsPositionRepository _gpsRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IGeofenceRepository _geofenceRepo;
    private readonly IGeofenceStateRepository _geofenceStateRepo;
    private readonly ITenantContext _tenant;
    private readonly INotificationService _notification;
    private readonly IMapper _mapper;

    public GpsService(
        IGpsPositionRepository gpsRepo,
        IVehicleRepository vehicleRepo,
        IGeofenceRepository geofenceRepo,
        IGeofenceStateRepository geofenceStateRepo,
        ITenantContext tenant,
        INotificationService notification,
        IMapper mapper)
    {
        _gpsRepo = gpsRepo;
        _vehicleRepo = vehicleRepo;
        _geofenceRepo = geofenceRepo;
        _geofenceStateRepo = geofenceStateRepo;
        _tenant = tenant;
        _notification = notification;
        _mapper = mapper;
    }

    /// <summary>Receive a single GPS position from a device.</summary>
    public async Task<GpsPositionResponse?> ReceivePositionAsync(ReceiveGpsPositionRequest request)
    {
        var vehicleId = request.VehicleId;
        if (!vehicleId.HasValue)
        {
            // Try to resolve vehicle by device ID
            var vehicles = await _vehicleRepo.GetAllAsync(await GetCompanyIdAsync());
            var vehicle = vehicles.FirstOrDefault(v => v.GpsDeviceId == request.DeviceId);
            if (vehicle == null) return null;
            vehicleId = vehicle.Id;
        }

        var position = new GpsPosition
        {
            VehicleId = vehicleId.Value,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Altitude = request.Altitude,
            Speed = request.Speed,
            Heading = request.Heading,
            GpsTimestamp = request.GpsTimestamp,
            IgnitionOn = request.IgnitionOn,
            Odometer = request.Odometer,
            FuelLevel = request.FuelLevel,
            Temperature = request.Temperature,
            DeviceBattery = request.DeviceBattery,
            GsmSignal = request.GsmSignal,
            Satellites = request.Satellites
        };

        await _gpsRepo.AddAsync(position);
        await _gpsRepo.SaveChangesAsync();

        // Geofence entry/exit tracking
        await ProcessGeofenceTransitionsAsync(vehicleId.Value, position);

        return _mapper.Map<GpsPositionResponse>(position);
    }

    /// <summary>Receive bulk GPS positions (batch from device/Traccar).</summary>
    public async Task<int> ReceiveBulkAsync(BulkReceiveGpsRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var deviceMap = vehicles.Where(v => v.GpsDeviceId != null)
            .ToDictionary(v => v.GpsDeviceId!, v => v.Id);

        var positions = new List<GpsPosition>();
        foreach (var req in request.Positions)
        {
            var vehicleId = req.VehicleId;
            if (!vehicleId.HasValue && deviceMap.TryGetValue(req.DeviceId, out var resolvedId))
                vehicleId = resolvedId;

            if (!vehicleId.HasValue) continue;

            positions.Add(new GpsPosition
            {
                VehicleId = vehicleId.Value,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                Altitude = req.Altitude,
                Speed = req.Speed,
                Heading = req.Heading,
                GpsTimestamp = req.GpsTimestamp,
                IgnitionOn = req.IgnitionOn,
                Odometer = req.Odometer,
                FuelLevel = req.FuelLevel,
                Temperature = req.Temperature,
                DeviceBattery = req.DeviceBattery,
                GsmSignal = req.GsmSignal,
                Satellites = req.Satellites
            });
        }

        if (positions.Count > 0)
            await _gpsRepo.AddRangeAsync(positions);

        await _gpsRepo.SaveChangesAsync();

        // Process geofence transitions in bulk — single pass per vehicle
        await ProcessBulkGeofenceTransitionsAsync(positions);

        return positions.Count;
    }

    /// <summary>Get latest position for all vehicles (real-time map).</summary>
    public async Task<List<VehiclePositionSummary>> GetFleetPositionsAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var latest = await _gpsRepo.GetLatestPerVehicleAsync(companyId);

        return latest.Select(g => new VehiclePositionSummary(
            g.VehicleId,
            g.Vehicle.Plate,
            $"{g.Vehicle.Brand.Name} {g.Vehicle.Model}",
            g.Latitude,
            g.Longitude,
            g.Speed,
            g.Heading,
            g.GpsTimestamp,
            g.IgnitionOn,
            g.FuelLevel,
            g.Vehicle.Driver?.FullName)).ToList();
    }

    /// <summary>Get GPS history for a specific vehicle.</summary>
    public async Task<GpsHistoryResponse?> GetVehicleHistoryAsync(Guid vehicleId, DateTime from, DateTime to)
    {
        var positions = await _gpsRepo.GetByVehicleAndDateRangeAsync(vehicleId, from, to);
        if (positions.Count == 0) return null;

        var vehicle = positions.First().Vehicle;
        return new GpsHistoryResponse(
            vehicleId,
            vehicle.Plate,
            positions.Select(p => _mapper.Map<GpsPositionResponse>(p)).ToList());
    }

    /// <summary>Get latest position for a specific vehicle.</summary>
    public async Task<GpsPositionResponse?> GetLatestPositionAsync(Guid vehicleId)
    {
        var pos = await _gpsRepo.GetLatestByVehicleAsync(vehicleId);
        return pos == null ? null : _mapper.Map<GpsPositionResponse>(pos);
    }

    /// <summary>Check if a point is inside any active geofence.</summary>
    public async Task<GeofenceCheckResponse> CheckPointInGeofenceAsync(double latitude, double longitude)
    {
        var geofences = await _geofenceRepo.GetActiveAsync();
        foreach (var gf in geofences)
        {
            if (IsInsideGeofence(latitude, longitude, gf))
                return new GeofenceCheckResponse(true, gf.Name, gf.Id);
        }
        return new GeofenceCheckResponse(false, null, null);
    }

    /// <summary>Clean up old GPS positions (older than N days).</summary>
    public async Task<int> CleanupOldPositionsAsync(int olderThanDays = 90)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        return await _gpsRepo.DeleteOlderThanAsync(cutoff);
    }

    // ── Geofence Entry/Exit Tracking ──

    private async Task ProcessGeofenceTransitionsAsync(Guid vehicleId, GpsPosition position)
    {
        try
        {
            var geofences = await _geofenceRepo.GetActiveAsync();
            var activeStates = await _geofenceStateRepo.GetActiveByVehicleAsync(vehicleId);
            bool hasChanges = false;

            foreach (var geofence in geofences)
            {
                var isCurrentlyInside = IsInsideGeofence(position.Latitude, position.Longitude, geofence);
                var existingState = activeStates.FirstOrDefault(s => s.GeofenceId == geofence.Id);

                if (isCurrentlyInside && existingState == null)
                {
                    // ── ENTRY ──
                    var state = new VehicleGeofenceState
                    {
                        VehicleId = vehicleId,
                        GeofenceId = geofence.Id,
                        EnteredAt = position.GpsTimestamp,
                        IsInside = true,
                        LastPositionId = position.Id
                    };
                    await _geofenceStateRepo.AddAsync(state);
                    hasChanges = true;
                    await NotifyGeofenceEventAsync(vehicleId, geofence.Name, "entry", position.GpsTimestamp);
                }
                else if (!isCurrentlyInside && existingState != null)
                {
                    // ── EXIT ──
                    existingState.IsInside = false;
                    existingState.ExitedAt = position.GpsTimestamp;
                    existingState.LastPositionId = position.Id;
                    await _geofenceStateRepo.UpdateAsync(existingState);
                    hasChanges = true;
                    await NotifyGeofenceEventAsync(vehicleId, geofence.Name, "exit", position.GpsTimestamp);
                }
            }

            if (hasChanges)
                await _geofenceStateRepo.SaveChangesAsync();
        }
        catch
        {
            // Geofence tracking is non-critical — never block GPS position saving
        }
    }

    private async Task ProcessBulkGeofenceTransitionsAsync(List<GpsPosition> positions)
    {
        try
        {
            var geofences = await _geofenceRepo.GetActiveAsync();
            if (geofences.Count == 0) return;

            bool hasChanges = false;
            var vehicleGroups = positions.GroupBy(p => p.VehicleId);

            foreach (var group in vehicleGroups)
            {
                var vehicleId = group.Key;
                var activeStates = await _geofenceStateRepo.GetActiveByVehicleAsync(vehicleId);
                var lastPosition = group.OrderByDescending(p => p.GpsTimestamp).First();

                foreach (var geofence in geofences)
                {
                    var isCurrentlyInside = IsInsideGeofence(lastPosition.Latitude, lastPosition.Longitude, geofence);
                    var existingState = activeStates.FirstOrDefault(s => s.GeofenceId == geofence.Id);

                    if (isCurrentlyInside && existingState == null)
                    {
                        await _geofenceStateRepo.AddAsync(new VehicleGeofenceState
                        {
                            VehicleId = vehicleId,
                            GeofenceId = geofence.Id,
                            EnteredAt = lastPosition.GpsTimestamp,
                            IsInside = true,
                            LastPositionId = lastPosition.Id
                        });
                        hasChanges = true;
                        await NotifyGeofenceEventAsync(vehicleId, geofence.Name, "entry", lastPosition.GpsTimestamp);
                    }
                    else if (!isCurrentlyInside && existingState != null)
                    {
                        existingState.IsInside = false;
                        existingState.ExitedAt = lastPosition.GpsTimestamp;
                        existingState.LastPositionId = lastPosition.Id;
                        await _geofenceStateRepo.UpdateAsync(existingState);
                        hasChanges = true;
                        await NotifyGeofenceEventAsync(vehicleId, geofence.Name, "exit", lastPosition.GpsTimestamp);
                    }
                }
            }

            if (hasChanges)
                await _geofenceStateRepo.SaveChangesAsync();
        }
        catch
        {
            // Geofence tracking is non-critical — never block GPS position saving
        }
    }

    private async Task NotifyGeofenceEventAsync(Guid vehicleId, string geofenceName, string eventType, DateTime timestamp)
    {
        var (title, message) = eventType == "entry"
            ? ("Vehículo entró a geocerca", $"El vehículo {vehicleId} entró a la geocerca '{geofenceName}' el {timestamp:dd/MM/yyyy HH:mm}.")
            : ("Vehículo salió de geocerca", $"El vehículo {vehicleId} salió de la geocerca '{geofenceName}' el {timestamp:dd/MM/yyyy HH:mm}.");

        var tenantId = _tenant.TenantId.ToString();
        await _notification.NotifyTenantAsync(tenantId, title, message, "fleet_geofence", vehicleId.ToString());
    }

    // ── Geofence containment check ──

    private static bool IsInsideGeofence(double lat, double lng, Geofence geofence)
    {
        if (geofence.Type == "Circle" && geofence.Radius.HasValue)
        {
            var coords = JsonSerializer.Deserialize<List<double[]>>(geofence.CoordinatesJson);
            if (coords != null && coords.Count > 0)
            {
                var centerLat = coords[0][0];
                var centerLng = coords[0][1];
                var distance = HaversineDistance(lat, lng, centerLat, centerLng);
                return distance <= geofence.Radius.Value;
            }
        }
        else if (geofence.Type == "Polygon")
        {
            var coords = JsonSerializer.Deserialize<List<double[]>>(geofence.CoordinatesJson);
            if (coords != null && coords.Count >= 3)
                return PointInPolygon(lat, lng, coords);
        }
        return false;
    }

    /// <summary>Haversine distance in kilometers.</summary>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double deg) => deg * Math.PI / 180;

    /// <summary>Ray-casting algorithm for point-in-polygon.</summary>
    private static bool PointInPolygon(double lat, double lng, List<double[]> polygon)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if ((polygon[i][0] > lat) != (polygon[j][0] > lat) &&
                lng < (polygon[j][1] - polygon[i][1]) * (lat - polygon[i][0]) / (polygon[j][0] - polygon[i][0]) + polygon[i][1])
                inside = !inside;
        }
        return inside;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        if (Guid.TryParse(_tenant.TenantId.ToString(), out var companyId))
            return companyId;
        return Guid.Empty;
    }
}
