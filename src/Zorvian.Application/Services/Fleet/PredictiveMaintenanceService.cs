using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Predictive maintenance service.
/// Analyzes work order history, maintenance schedules, and vehicle mileage
/// to forecast when components will need service and flag high-risk vehicles.
/// </summary>
public sealed class PredictiveMaintenanceService
{
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IWorkOrderRepository _workOrderRepo;
    private readonly IMaintenanceScheduleRepository _scheduleRepo;
    private readonly ITenantContext _tenant;

    // Component thresholds (km-based defaults)
    private static readonly Dictionary<string, decimal> DefaultKmThresholds = new()
    {
        ["Engine Oil"]       = 5_000,
        ["Oil Filter"]       = 5_000,
        ["Air Filter"]       = 10_000,
        ["Brake Pads"]       = 30_000,
        ["Brake Discs"]      = 60_000,
        ["Tires"]            = 40_000,
        ["Transmission Fluid"] = 40_000,
        ["Coolant"]          = 30_000,
        ["Spark Plugs"]      = 20_000,
        ["Belt"]             = 50_000,
        ["Battery"]          = 100_000,
    };

    // Date-based default interval (days)
    private const int DefaultDateIntervalDays = 180;

    // Warning thresholds
    private const int DueSoonKmThreshold = 500;
    private const int DueSoonDaysThreshold = 30;

    public PredictiveMaintenanceService(
        IVehicleRepository vehicleRepo,
        IWorkOrderRepository workOrderRepo,
        IMaintenanceScheduleRepository scheduleRepo,
        ITenantContext tenant)
    {
        _vehicleRepo = vehicleRepo;
        _workOrderRepo = workOrderRepo;
        _scheduleRepo = scheduleRepo;
        _tenant = tenant;
    }

    /// <summary>
    /// Generate maintenance forecasts for all vehicles (or one specific vehicle).
    /// </summary>
    public async Task<List<VehicleMaintenanceForecast>> GetMaintenanceForecastsAsync(Guid? vehicleId = null)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];

        var vehicles = vehicleId.HasValue
            ? new[] { await _vehicleRepo.GetByIdAsync(vehicleId.Value) }
                .Where(v => v != null).Cast<Core.Entities.Fleet.Vehicle>().ToList()
            : (await _vehicleRepo.GetAllAsync(companyId)).Where(v => v.Status == "Active").ToList();

        var forecasts = new List<VehicleMaintenanceForecast>();
        foreach (var vehicle in vehicles)
        {
            forecasts.Add(await BuildVehicleForecastAsync(vehicle, companyId));
        }
        return forecasts.OrderByDescending(f => f.RiskScore).ToList();
    }

    /// <summary>
    /// Get a summary of fleet-wide maintenance risk.
    /// </summary>
    public async Task<PredictiveMaintenanceSummary> GetSummaryAsync()
    {
        var forecasts = await GetMaintenanceForecastsAsync();
        var overdue = forecasts.Count(f => f.Components.Any(c => c.Status == "Overdue"));
        var dueSoon = forecasts.Count(f => f.Components.Any(c => c.Status == "DueSoon"));
        var highRisk = forecasts.Count(f => f.RiskLevel is "High" or "Critical");

        return new PredictiveMaintenanceSummary(
            TotalVehicles: forecasts.Count,
            VehiclesOverdue: overdue,
            VehiclesDueSoon: dueSoon,
            HighRiskVehicles: highRisk,
            AvgRiskScore: forecasts.Count > 0
                ? (decimal)Math.Round(forecasts.Average(f => f.RiskScore), 1)
                : 0m,
            TopRiskVehicles: forecasts.Take(10).ToList()
        );
    }

    // ── Private helpers ──

    private async Task<VehicleMaintenanceForecast> BuildVehicleForecastAsync(Core.Entities.Fleet.Vehicle vehicle, Guid companyId)
    {
        var components = new List<ComponentForecast>();
        var now = DateTime.UtcNow;

        // 1. Check scheduled maintenance
        var schedules = await _scheduleRepo.GetAllAsync(companyId);
        var vehicleSchedules = schedules.Where(s => s.VehicleId == vehicle.Id && s.Status == "Active").ToList();

        foreach (var schedule in vehicleSchedules)
        {
            var component = BuildFromSchedule(schedule, vehicle.CurrentKm, now);
            components.Add(component);
        }

        // 2. Estimate components not in schedule based on work order history
        var workOrders = await _workOrderRepo.GetAllAsync(companyId);
        var vehicleWorkOrders = workOrders.Where(w => w.VehicleId == vehicle.Id).ToList();

        // Check which components have schedules; add predictions for those that don't
        var scheduledComponentNames = vehicleSchedules
            .Select(s => s.Template?.Name)
            .Where(n => n != null)
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var (componentName, kmThreshold) in DefaultKmThresholds)
        {
            if (scheduledComponentNames.Contains(componentName)) continue;

            // Estimate from work order history
            var lastRelevantWorkOrder = vehicleWorkOrders
                .Where(w => w.SolutionApplied?.Contains(componentName, StringComparison.OrdinalIgnoreCase) == true)
                .OrderByDescending(w => w.EndDate ?? w.CreatedAt)
                .FirstOrDefault();

            var component = BuildFromHistory(componentName, kmThreshold, lastRelevantWorkOrder, vehicle.CurrentKm, now);
            components.Add(component);
        }

        // 3. Calculate risk score
        var riskScore = CalculateRiskScore(components);
        var riskLevel = riskScore switch
        {
            >= 80 => "Critical",
            >= 60 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

        return new VehicleMaintenanceForecast(
            VehicleId: vehicle.Id,
            Plate: vehicle.Plate,
            BrandModel: $"{vehicle.Brand?.Name} {vehicle.Model}",
            CurrentKm: vehicle.CurrentKm,
            CurrentDate: now,
            Components: components.OrderByDescending(c => c.Status == "Overdue" ? 0 :
                c.Status == "DueSoon" ? 1 : c.Status == "Scheduled" ? 2 : 3).ToList(),
            RiskScore: riskScore,
            RiskLevel: riskLevel
        );
    }

    private static ComponentForecast BuildFromSchedule(
        Core.Entities.Fleet.MaintenanceSchedule schedule,
        decimal currentKm,
        DateTime now)
    {
        var kmUntilDue = schedule.NextExecutionKm.HasValue
            ? (int)(schedule.NextExecutionKm.Value - currentKm)
            : int.MaxValue;

        var daysUntilDue = schedule.NextExecutionDate.HasValue
            ? (schedule.NextExecutionDate.Value - now).Days
            : int.MaxValue;

        var (status, confidence) = (kmUntilDue, daysUntilDue, schedule.ScheduleType) switch
        {
            _ when kmUntilDue <= 0 || daysUntilDue <= 0 => ("Overdue", 0.95m),
            _ when kmUntilDue <= DueSoonKmThreshold || daysUntilDue <= DueSoonDaysThreshold => ("DueSoon", 0.90m),
            _ when schedule.Status == "Active" => ("Scheduled", 0.80m),
            _ => ("Healthy", 0.70m),
        };

        return new ComponentForecast(
            ComponentName: schedule.Template?.Name ?? "Scheduled Maintenance",
            ScheduleType: schedule.ScheduleType,
            CurrentValue: schedule.ScheduleType == "Km" ? currentKm : null,
            ThresholdValue: schedule.NextExecutionKm,
            NextThresholdValue: schedule.NextExecutionKm,
            NextDate: schedule.NextExecutionDate,
            DaysUntilDue: Math.Max(0, daysUntilDue),
            KmUntilDue: Math.Max(0, kmUntilDue),
            Status: status,
            ConfidenceScore: confidence
        );
    }

    private static ComponentForecast BuildFromHistory(
        string componentName,
        decimal kmThreshold,
        Core.Entities.Fleet.WorkOrder? lastWorkOrder,
        decimal currentKm,
        DateTime now)
    {
        if (lastWorkOrder == null)
        {
            // No history — estimate from general threshold
            return new ComponentForecast(
                ComponentName: componentName,
                ScheduleType: "Km",
                CurrentValue: currentKm,
                ThresholdValue: kmThreshold,
                NextThresholdValue: kmThreshold,
                NextDate: null,
                DaysUntilDue: int.MaxValue,
                KmUntilDue: (int)kmThreshold,
                Status: "Healthy",
                ConfidenceScore: 0.40m // Low confidence without data
            );
        }

        var lastServiceKm = lastWorkOrder.Vehicle?.CurrentKm ?? currentKm;
        var kmSinceLastService = currentKm - lastServiceKm;
        var kmUntilDue = (int)(kmThreshold - kmSinceLastService);
        var daysSinceLastService = (now - (lastWorkOrder.EndDate ?? lastWorkOrder.CreatedAt)).Days;
        var kmPerDay = daysSinceLastService > 0 ? kmSinceLastService / daysSinceLastService : 0m;
        var estimatedDaysUntilDue = kmPerDay > 0 ? (int)(kmThreshold / kmPerDay - daysSinceLastService) : int.MaxValue;

        var status = kmUntilDue <= 0 ? "Overdue"
            : kmUntilDue <= DueSoonKmThreshold ? "DueSoon"
            : "Healthy";

        // Confidence increases with more service history data
        var confidence = Math.Min(0.85m, 0.40m + (lastWorkOrder.CostTotal > 0 ? 0.15m : 0));

        return new ComponentForecast(
            ComponentName: componentName,
            ScheduleType: "Km",
            CurrentValue: currentKm,
            ThresholdValue: kmThreshold,
            NextThresholdValue: kmThreshold,
            NextDate: estimatedDaysUntilDue > 0 ? now.AddDays(estimatedDaysUntilDue) : null,
            DaysUntilDue: Math.Max(0, estimatedDaysUntilDue),
            KmUntilDue: Math.Max(0, kmUntilDue),
            Status: status,
            ConfidenceScore: confidence
        );
    }

    private static int CalculateRiskScore(List<ComponentForecast> components)
    {
        if (components.Count == 0) return 0;

        var overdueCount = components.Count(c => c.Status == "Overdue");
        var dueSoonCount = components.Count(c => c.Status == "DueSoon");
        var total = components.Count;

        // Weighted score: overdue items are 3x, due soon items are 1.5x
        var weightedSum = overdueCount * 30.0 + dueSoonCount * 15.0;
        var maxPossible = total * 30.0;

        var score = (int)Math.Min(100, (weightedSum / maxPossible) * 100);

        // Urgency modifier: if any component is severely overdue
        var overdueItems = components
            .Where(c => c.Status == "Overdue")
            .ToList();
        if (overdueItems.Count > 0)
        {
            var maxOverdue = overdueItems.Max(c => c.KmUntilDue < 0 ? Math.Abs(c.KmUntilDue) : 0);
            if (maxOverdue > 2000) score = Math.Min(100, score + 20);
        }

        return score;
    }
}
