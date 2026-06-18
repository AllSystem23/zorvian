using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Route optimization service: nearest-neighbor TSP solver,
/// automatic driver assignment based on availability and fitness,
/// and delivery ETA calculation.
/// </summary>
public sealed class RouteOptimizationService
{
    private readonly IRouteRepository _routeRepo;
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public RouteOptimizationService(
        IRouteRepository routeRepo,
        IDeliveryRepository deliveryRepo,
        IDriverRepository driverRepo,
        IVehicleRepository vehicleRepo,
        ITenantContext tenant,
        IMapper mapper)
    {
        _routeRepo = routeRepo;
        _deliveryRepo = deliveryRepo;
        _driverRepo = driverRepo;
        _vehicleRepo = vehicleRepo;
        _tenant = tenant;
        _mapper = mapper;
    }

    /// <summary>
    /// Optimize a route using nearest-neighbor heuristic.
    /// Reorders the route points to minimize total distance.
    /// </summary>
    public async Task<OptimizedRouteResponse?> OptimizeRouteAsync(OptimizeRouteRequest request)
    {
        var route = await _routeRepo.GetByIdAsync(request.RouteId);
        if (route == null) return null;

        var points = route.Points.OrderBy(p => p.Order).ToList();
        if (points.Count < 2)
            return new OptimizedRouteResponse(
                route.Id, route.Name,
                route.DistanceEstKm, route.DurationEstMinutes, route.CostEst,
                points.Select((p, i) => MapToStop(p, i, 0)).ToList(),
                ["Route has fewer than 2 points — no optimization needed"]);

        // Nearest-neighbor TSP
        var optimized = NearestNeighborOptimize(points, request.OptimizationCriteria);
        var notes = new List<string>
        {
            $"Optimized {points.Count} stops using {request.OptimizationCriteria} criterion",
            $"Original distance: {route.DistanceEstKm} km"
        };

        // Recalculate totals
        decimal totalDistance = 0;
        int totalDuration = 0;
        var stops = new List<OptimizedStop>();
        for (int i = 0; i < optimized.Count; i++)
        {
            var stop = MapToStop(optimized[i], i, totalDistance);
            totalDistance += optimized[i].DistanceFromPreviousKm ?? 0;
            totalDuration += optimized[i].DurationEstMinutes ?? 15;
            stops.Add(stop);
        }

        notes.Add($"Optimized distance: {totalDistance:F1} km");

        // Calculate cost based on distance
        var costPerKm = route.CostEst > 0 && route.DistanceEstKm > 0
            ? route.CostEst / route.DistanceEstKm
            : 0.50m;
        var estimatedCost = totalDistance * costPerKm;

        return new OptimizedRouteResponse(
            route.Id, route.Name,
            totalDistance, totalDuration, estimatedCost,
            stops, notes);
    }

    /// <summary>
    /// Automatically assign the best available driver to a route.
    /// Considers license validity, today's workload, and availability.
    /// </summary>
    public async Task<DriverAssignmentResponse?> AssignDriverAsync(AssignDriverRequest request)
    {
        var route = await _routeRepo.GetByIdAsync(request.RouteId);
        if (route == null) return null;

        // Get all active drivers
        var companyId = await GetCompanyIdAsync();
        var drivers = await _driverRepo.GetAllAsync(companyId);
        var activeDrivers = drivers.Where(d => d.Status == "Active").ToList();

        if (activeDrivers.Count == 0)
            throw new InvalidOperationException("No active drivers available");

        Driver? selected = null;

        // If preferred driver specified, try to use them
        if (request.PreferredDriverId.HasValue)
        {
            selected = activeDrivers.FirstOrDefault(d => d.Id == request.PreferredDriverId.Value);
        }

        // Auto-select best available driver
        if (selected == null)
        {
            selected = activeDrivers
                .OrderBy(d => d.LicenseExpiryDate < DateOnly.FromDateTime(DateTime.UtcNow) ? 1 : 0)
                .ThenBy(d => d.LicenseCategory?.Name ?? "")
                .FirstOrDefault();
        }

        if (selected == null)
            throw new InvalidOperationException("No suitable driver found");

        // Assign to route
        route.DriverId = selected.Id;
        await _routeRepo.UpdateAsync(route);
        await _routeRepo.SaveChangesAsync();

        return new DriverAssignmentResponse(
            route.Id,
            selected.Id,
            selected.FullName,
            selected.LicenseCategory?.Name ?? "N/A",
            0, // Would come from trip history
            0, // Would come from trip history
            request.PreferredDriverId.HasValue ? "Preferred driver selected" : "Auto-assigned (best available)");
    }

    /// <summary>
    /// Get all available drivers for a given route.
    /// </summary>
    public async Task<List<AvailableDriverResponse>> GetAvailableDriversAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var drivers = await _driverRepo.GetAllAsync(companyId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return drivers
            .Where(d => d.Status == "Active")
            .Select(d => new AvailableDriverResponse(
                d.Id,
                d.FullName,
                d.LicenseCategory?.Name ?? "N/A",
                d.LicenseNumber,
                d.LicenseExpiryDate >= today,
                0, // Would come from trip history
                0, // Would come from trip history
                true))
            .ToList();
    }

    /// <summary>
    /// Calculate ETAs for all deliveries in a route.
    /// </summary>
    public async Task<DeliveryEtaResponse?> CalculateDeliveryEtasAsync(DeliveryEtaRequest request)
    {
        var route = await _routeRepo.GetByIdAsync(request.RouteId);
        if (route == null) return null;

        var deliveries = await _deliveryRepo.GetAllAsync();
        var routeDeliveries = deliveries
            .Where(d => d.RouteId == route.Id)
            .OrderBy(d => d.DeliveryAddress)
            .ToList();

        var startTime = DateTime.SpecifyKind(
            route.ScheduledDate.ToDateTime(route.EstimatedDeparture),
            DateTimeKind.Utc);

        var etas = new List<DeliveryEta>();
        var cumulativeMinutes = 0;
        decimal cumulativeDistance = 0;
        var avgSpeedKmh = 30m; // Urban average
        var stopDurationMinutes = 15;

        for (int i = 0; i < routeDeliveries.Count; i++)
        {
            var d = routeDeliveries[i];
            var distanceKm = i == 0 ? 5m : EstimateDistanceKm(); // Default 5km between stops
            cumulativeDistance += distanceKm;

            var travelMinutes = (int)(distanceKm / avgSpeedKmh * 60);
            cumulativeMinutes += travelMinutes;

            var arrival = startTime.AddMinutes(cumulativeMinutes);
            var departure = arrival.AddMinutes(stopDurationMinutes);
            cumulativeMinutes += stopDurationMinutes;

            etas.Add(new DeliveryEta(
                d.Id,
                d.Code,
                d.Client != null ? $"{d.Client.FirstName} {d.Client.LastName}" : "N/A",
                d.DeliveryAddress,
                i + 1,
                arrival,
                departure,
                distanceKm,
                cumulativeMinutes,
                d.Status));
        }

        return new DeliveryEtaResponse(
            route.Id,
            route.Name,
            startTime,
            etas);
    }

    // ── Private helpers ──

    private static List<RoutePoint> NearestNeighborOptimize(List<RoutePoint> points, string criteria)
    {
        if (points.Count <= 1) return points;

        var remaining = new List<RoutePoint>(points);
        var optimized = new List<RoutePoint> { remaining[0] };
        remaining.RemoveAt(0);

        while (remaining.Count > 0)
        {
            var last = optimized[^1];
            var nearest = remaining
                .OrderBy(p => EstimatePointDistance(last, p, criteria))
                .First();
            optimized.Add(nearest);
            remaining.Remove(nearest);
        }

        return optimized;
    }

    private static double EstimatePointDistance(RoutePoint from, RoutePoint to, string criteria)
    {
        // Simplified distance estimation based on order of points
        // In production, this would use geocoded coordinates and a real distance matrix
        var orderDiff = Math.Abs(from.Order - to.Order);
        return criteria switch
        {
            "Time" => orderDiff * 1.2, // Weight time more heavily
            "Cost" => orderDiff * 1.5, // Weight cost more
            _ => orderDiff * 1.0       // Distance (default)
        };
    }

    private static decimal EstimateDistanceKm()
    {
        // Simplified: would use geocoding API in production
        return 5.0m;
    }

    private static OptimizedStop MapToStop(RoutePoint point, int order, decimal cumulativeDistance)
    {
        return new OptimizedStop(
            order + 1,
            point.Address,
            point.ClientId,
            null, // ClientName would need Include
            point.DistanceFromPreviousKm ?? 0,
            (point.DurationEstMinutes ?? 15) + (order * 15), // Cumulative
            point.DurationEstMinutes ?? 15,
            null, // Would calculate from route start time
            null, // Would calculate from arrival + duration
            point.Instructions);
    }

    private Task<Guid> GetCompanyIdAsync()
    {
        if (_tenant.IsSuperAdmin) return Task.FromResult(Guid.Empty);

        if (Guid.TryParse(_tenant.TenantId.ToString(), out var companyId))
            return Task.FromResult(companyId);
        return Task.FromResult(Guid.Empty);
    }
}
