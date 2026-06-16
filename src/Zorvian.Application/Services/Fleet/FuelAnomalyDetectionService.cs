using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

/// <summary>
/// Fuel anomaly detection service.
/// Analyzes fuel consumption patterns per vehicle to detect anomalies:
/// high consumption, sudden increases, price outliers, frequency anomalies.
/// Uses statistical methods (Z-score, moving averages) without external ML dependencies.
/// </summary>
public sealed class FuelAnomalyDetectionService
{
    private readonly IFuelRefillRepository _fuelRepo;
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly ITenantContext _tenant;

    // Thresholds for anomaly detection
    private const decimal ZScoreThresholdHigh = 2.5m;   // 2.5 std devs = high severity
    private const decimal ZScoreThresholdMedium = 2.0m;  // 2.0 std devs = medium severity
    private const decimal ZScoreThresholdLow = 1.5m;     // 1.5 std devs = low severity
    private const decimal PriceOutlierMultiplier = 1.3m;  // 30% above avg = outlier
    private const decimal ConsumptionSpikeMultiplier = 1.5m; // 50% above avg = spike
    private const int MinSampleSize = 5;                  // Minimum refills for statistical analysis

    public FuelAnomalyDetectionService(
        IFuelRefillRepository fuelRepo,
        IVehicleRepository vehicleRepo,
        IDriverRepository driverRepo,
        ITenantContext tenant)
    {
        _fuelRepo = fuelRepo;
        _vehicleRepo = vehicleRepo;
        _driverRepo = driverRepo;
        _tenant = tenant;
    }

    /// <summary>
    /// Analyze fuel consumption and detect anomalies across all vehicles (or one specific vehicle).
    /// </summary>
    public async Task<FuelAnomalySummary> AnalyzeAsync(AnalyzeFuelConsumptionRequest request)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return new FuelAnomalySummary(0, 0, 0, 0, 0, 0, []);

        var refills = await _fuelRepo.GetAllAsync(companyId);
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-6);
        var toDate = request.ToDate ?? DateTime.UtcNow;

        var filteredRefills = refills
            .Where(f => f.RefillDateTime >= fromDate && f.RefillDateTime <= toDate && f.ValidForCalculation)
            .ToList();

        if (request.VehicleId.HasValue)
            filteredRefills = filteredRefills.Where(f => f.VehicleId == request.VehicleId.Value).ToList();

        // Group by vehicle for per-vehicle analysis
        var groupedByVehicle = filteredRefills.GroupBy(f => f.VehicleId).ToList();
        var allAnomalies = new List<FuelAnomalyResult>();

        foreach (var vehicleGroup in groupedByVehicle)
        {
            var vehicleRefills = vehicleGroup.OrderBy(f => f.RefillDateTime).ToList();
            var anomalies = DetectAnomaliesForVehicle(vehicleRefills);
            allAnomalies.AddRange(anomalies);
        }

        // Estimate monthly waste from high-severity anomalies
        var highSeverityWaste = allAnomalies
            .Where(a => a.Severity == "High")
            .Sum(a => Math.Abs(a.Liters - a.ExpectedLiters) * a.TotalCost / Math.Max(a.Liters, 0.01m));

        return new FuelAnomalySummary(
            TotalRefillsAnalyzed: filteredRefills.Count,
            AnomaliesDetected: allAnomalies.Count,
            HighSeverity: allAnomalies.Count(a => a.Severity == "High"),
            MediumSeverity: allAnomalies.Count(a => a.Severity == "Medium"),
            LowSeverity: allAnomalies.Count(a => a.Severity == "Low"),
            EstimatedMonthlyWaste: Math.Round(highSeverityWaste / Math.Max(1, (toDate - fromDate).Days / 30m), 2),
            RecentAnomalies: allAnomalies.OrderByDescending(a => a.RefillDate).Take(50).ToList()
        );
    }

    /// <summary>
    /// Get consumption trends per vehicle (avg, min, max, std deviation, daily data).
    /// </summary>
    public async Task<List<FuelConsumptionTrend>> GetConsumptionTrendsAsync(Guid? vehicleId = null)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];

        var refills = await _fuelRepo.GetAllAsync(companyId);
        var fromDate = DateTime.UtcNow.AddMonths(-6);
        var filteredRefills = refills
            .Where(f => f.RefillDateTime >= fromDate && f.ValidForCalculation)
            .ToList();

        if (vehicleId.HasValue)
            filteredRefills = filteredRefills.Where(f => f.VehicleId == vehicleId.Value).ToList();

        var groupedByVehicle = filteredRefills.GroupBy(f => f.VehicleId).ToList();
        var trends = new List<FuelConsumptionTrend>();

        foreach (var vehicleGroup in groupedByVehicle)
        {
            var vehicleRefills = vehicleGroup.OrderBy(f => f.RefillDateTime).ToList();
            if (vehicleRefills.Count < 2) continue;

            var consumptions = CalculateConsumptionsPer100Km(vehicleRefills);
            if (consumptions.Count == 0) continue;

            var avg = consumptions.Average();
            var stdDev = CalculateStdDev(consumptions);
            var dailyData = AggregateByDay(vehicleRefills);

            // Trend: compare last 30 days avg vs prior 30 days avg
            var now = DateTime.UtcNow;
            var recent = vehicleRefills.Where(f => f.RefillDateTime >= now.AddDays(-30)).ToList();
            var prior = vehicleRefills.Where(f => f.RefillDateTime >= now.AddDays(-60) && f.RefillDateTime < now.AddDays(-30)).ToList();

            var recentConsumptions = CalculateConsumptionsPer100Km(recent);
            var priorConsumptions = CalculateConsumptionsPer100Km(prior);
            var trend = (recentConsumptions.Count > 0 && priorConsumptions.Count > 0)
                ? recentConsumptions.Average() - priorConsumptions.Average()
                : 0m;

            var vehicle = vehicleRefills[0].Vehicle;

            trends.Add(new FuelConsumptionTrend(
                VehicleId: vehicleGroup.Key,
                Plate: vehicle?.Plate ?? "N/A",
                AvgConsumptionPer100Km: Math.Round(avg, 2),
                MinConsumptionPer100Km: Math.Round(consumptions.Min(), 2),
                MaxConsumptionPer100Km: Math.Round(consumptions.Max(), 2),
                StdDeviation: Math.Round(stdDev, 2),
                SampleCount: consumptions.Count,
                CurrentTrend: Math.Round(trend, 2),
                DailyData: dailyData
            ));
        }

        return trends.OrderByDescending(t => t.CurrentTrend).ToList();
    }

    /// <summary>
    /// Mark a fuel refill as anomaly or not (for manual overrides).
    /// </summary>
    public async Task<bool> MarkAnomalyAsync(Guid refillId, MarkFuelAnomalyRequest request)
    {
        var refill = await _fuelRepo.GetByIdAsync(refillId);
        if (refill == null) return false;

        refill.AnomalyFlag = request.IsAnomaly;
        refill.AnomalyNotes = request.Notes;
        await _fuelRepo.UpdateAsync(refill);
        await _fuelRepo.SaveChangesAsync();
        return true;
    }

    // ── Private detection methods ──

    private List<FuelAnomalyResult> DetectAnomaliesForVehicle(List<Core.Entities.Fleet.FuelRefill> refills)
    {
        var anomalies = new List<FuelAnomalyResult>();
        if (refills.Count < MinSampleSize) return anomalies;

        var vehicle = refills[0].Vehicle;
        var consumptions = CalculateConsumptionsPer100Km(refills);
        if (consumptions.Count < MinSampleSize - 1) return anomalies;

        var avgConsumption = consumptions.Average();
        var stdDev = CalculateStdDev(consumptions);
        var avgPrice = refills.Average(f => f.PricePerLiter);

        for (int i = 0; i < refills.Count; i++)
        {
            var refill = refills[i];

            // Skip if km hasn't changed (can't calculate consumption)
            if (i > 0 && refills[i].CurrentKm <= refills[i - 1].CurrentKm) continue;

            // 1. Volume/Consumption anomaly (Z-score)
            if (i > 0 && consumptions.Count >= i)
            {
                var consumption = consumptions[Math.Min(i - 1, consumptions.Count - 1)];
                var zScore = stdDev > 0 ? (consumption - avgConsumption) / stdDev : 0;

                if (Math.Abs(zScore) >= ZScoreThresholdLow)
                {
                    var (severity, anomalyType) = zScore >= ZScoreThresholdHigh
                        ? ("High", "HighConsumption")
                        : zScore >= ZScoreThresholdMedium
                            ? ("Medium", "HighConsumption")
                            : ("Low", "HighConsumption");

                    anomalies.Add(BuildAnomalyResult(refill, vehicle, anomalyType, severity,
                        $"Consumo {consumption:F1} L/100km vs promedio {avgConsumption:F1} L/100km (Z={zScore:F2})",
                        avgConsumption, Math.Abs(zScore - 1) * 100));
                }
            }

            // 2. Sudden increase (compared to previous refill)
            if (i > 0)
            {
                var prevLiters = refills[i - 1].Liters;
                if (prevLiters > 0 && refill.Liters > prevLiters * ConsumptionSpikeMultiplier)
                {
                    var deviation = ((refill.Liters - prevLiters) / prevLiters) * 100;
                    anomalies.Add(BuildAnomalyResult(refill, vehicle, "SuddenIncrease",
                        deviation > 80 ? "High" : "Medium",
                        $"Aumento súbito: {refill.Liters:F1}L vs {prevLiters:F1}L anterior (+{deviation:F0}%)",
                        prevLiters, deviation));
                }
            }

            // 3. Price outlier
            if (Math.Abs(refill.PricePerLiter - avgPrice) > avgPrice * (PriceOutlierMultiplier - 1))
            {
                var priceDeviation = Math.Abs(refill.PricePerLiter - avgPrice) / avgPrice * 100;
                if (priceDeviation > 30)
                {
                    anomalies.Add(BuildAnomalyResult(refill, vehicle, "PriceOutlier",
                        priceDeviation > 50 ? "High" : "Medium",
                        $"Precio {refill.PricePerLiter:F2} vs promedio {avgPrice:F2} ({priceDeviation:F0}% diferencia)",
                        refill.Liters, priceDeviation));
                }
            }
        }

        // 4. Frequency anomaly (too many refills in short period)
        var recentRefills = refills.Where(f => f.RefillDateTime > DateTime.UtcNow.AddDays(-7)).ToList();
        if (recentRefills.Count > 7) // More than 1 per day
        {
            var firstRefill = recentRefills[0];
            anomalies.Add(BuildAnomalyResult(firstRefill, vehicle, "FrequencyAnomaly", "Medium",
                $"Frecuencia anormal: {recentRefills.Count} abastecimientos en 7 días",
                avgConsumption * recentRefills.Count, recentRefills.Count / 7m * 100));
        }

        return anomalies;
    }

    private static FuelAnomalyResult BuildAnomalyResult(
        Core.Entities.Fleet.FuelRefill refill,
        Core.Entities.Fleet.Vehicle? vehicle,
        string anomalyType,
        string severity,
        string description,
        decimal expectedLiters,
        decimal deviationPercent)
    {
        return new FuelAnomalyResult(
            VehicleId: refill.VehicleId,
            Plate: vehicle?.Plate ?? "N/A",
            BrandModel: vehicle != null ? $"{vehicle.Brand?.Name} {vehicle.Model}" : "N/A",
            DriverName: refill.Driver?.FullName ?? "N/A",
            RefillDate: refill.RefillDateTime,
            Liters: refill.Liters,
            TotalCost: refill.TotalCost,
            PricePerLiter: refill.PricePerLiter,
            CurrentKm: refill.CurrentKm,
            AnomalyType: anomalyType,
            Severity: severity,
            Description: description,
            ExpectedLiters: Math.Round(expectedLiters, 2),
            DeviationPercent: Math.Round(deviationPercent, 1)
        );
    }

    private static List<decimal> CalculateConsumptionsPer100Km(List<Core.Entities.Fleet.FuelRefill> orderedRefills)
    {
        var result = new List<decimal>();
        for (int i = 1; i < orderedRefills.Count; i++)
        {
            var kmDelta = orderedRefills[i].CurrentKm - orderedRefills[i - 1].CurrentKm;
            if (kmDelta <= 0) continue;
            var liters = orderedRefills[i].Liters;
            result.Add(Math.Round(liters / kmDelta * 100, 2));
        }
        return result;
    }

    private static List<DailyConsumption> AggregateByDay(List<Core.Entities.Fleet.FuelRefill> orderedRefills)
    {
        return orderedRefills
            .GroupBy(f => DateOnly.FromDateTime(f.RefillDateTime))
            .Select(g => new DailyConsumption(
                Date: g.Key,
                TotalLiters: Math.Round(g.Sum(f => f.Liters), 2),
                TotalKm: g.Max(f => f.CurrentKm) - g.Min(f => f.CurrentKm),
                ConsumptionPer100Km: 0, // Calculated on demand
                RefillCount: g.Count()
            ))
            .OrderBy(d => d.Date)
            .ToList();
    }

    private static decimal CalculateStdDev(List<decimal> values)
    {
        if (values.Count <= 1) return 0;
        var avg = values.Average();
        var sumSqDiff = values.Sum(v => (v - avg) * (v - avg));
        return (decimal)Math.Sqrt((double)(sumSqDiff / (values.Count - 1)));
    }
}
