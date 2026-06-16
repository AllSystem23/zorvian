namespace Zorvian.Application.DTOs.Fleet;

// ── Predictive Maintenance ──

public sealed record VehicleMaintenanceForecast(
    Guid VehicleId,
    string Plate,
    string BrandModel,
    decimal CurrentKm,
    DateTime CurrentDate,
    List<ComponentForecast> Components,
    int RiskScore,        // 0-100
    string RiskLevel      // Low, Medium, High, Critical
);

public sealed record ComponentForecast(
    string ComponentName,         // e.g. "Engine Oil", "Brake Pads", "Tires"
    string ScheduleType,          // Date, Km, HourMeter
    decimal? CurrentValue,        // Current km or hour meter
    decimal? ThresholdValue,      // When maintenance is due
    decimal? NextThresholdValue,  // Next threshold after maintenance
    DateTime? NextDate,           // If date-based
    int DaysUntilDue,
    int KmUntilDue,
    string Status,                // Overdue, DueSoon, Scheduled, Healthy
    decimal ConfidenceScore       // 0.0 - 1.0
);

public sealed record PredictiveMaintenanceSummary(
    int TotalVehicles,
    int VehiclesOverdue,
    int VehiclesDueSoon,          // Within 30 days / 500 km
    int HighRiskVehicles,
    decimal AvgRiskScore,
    List<VehicleMaintenanceForecast> TopRiskVehicles
);

// ── Fuel Anomaly Detection ──

public sealed record FuelAnomalyResult(
    Guid VehicleId,
    string Plate,
    string BrandModel,
    string DriverName,
    DateTime RefillDate,
    decimal Liters,
    decimal TotalCost,
    decimal PricePerLiter,
    decimal CurrentKm,
    string AnomalyType,           // HighConsumption, SuddenIncrease, PriceOutlier, FrequencyAnomaly, VolumeOutlier
    string Severity,              // Low, Medium, High
    string Description,
    decimal ExpectedLiters,
    decimal DeviationPercent
);

public sealed record FuelConsumptionTrend(
    Guid VehicleId,
    string Plate,
    decimal AvgConsumptionPer100Km,
    decimal MinConsumptionPer100Km,
    decimal MaxConsumptionPer100Km,
    decimal StdDeviation,
    int SampleCount,
    decimal CurrentTrend,         // Positive = increasing consumption (bad)
    List<DailyConsumption> DailyData
);

public sealed record DailyConsumption(
    DateOnly Date,
    decimal TotalLiters,
    decimal TotalKm,
    decimal ConsumptionPer100Km,
    int RefillCount
);

public sealed record FuelAnomalySummary(
    int TotalRefillsAnalyzed,
    int AnomaliesDetected,
    int HighSeverity,
    int MediumSeverity,
    int LowSeverity,
    decimal EstimatedMonthlyWaste,
    List<FuelAnomalyResult> RecentAnomalies
);

// ── Requests ──

public sealed record AnalyzeFuelConsumptionRequest(
    Guid? VehicleId,    // null = all vehicles
    DateTime? FromDate,
    DateTime? ToDate
);

public sealed record MarkFuelAnomalyRequest(
    bool IsAnomaly,
    string? Notes
);
