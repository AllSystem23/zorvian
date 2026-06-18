namespace Zorvian.Application.DTOs.Fleet;

// ── Report Request DTOs ──

public sealed record FleetReportRequest(
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? CategoryId,
    string? Currency
);

// ── Operational Report Responses ──

public sealed record VehicleUsageReportRow(
    Guid VehicleId,
    string Plate,
    string BrandModel,
    string Status,
    decimal TotalKm,
    int TripCount,
    decimal TotalHours,
    decimal AverageKmPerTrip
);

public sealed record VehicleUsageReport(
    List<VehicleUsageReportRow> Vehicles,
    decimal GrandTotalKm,
    int GrandTotalTrips,
    decimal GrandTotalHours
);

public sealed record DeliveryReportRow(
    string Status,
    int Count,
    decimal Percentage
);

public sealed record DeliveryReport(
    List<DeliveryReportRow> ByStatus,
    int TotalDeliveries,
    int CompletedOnTime,
    decimal OnTimeRate
);

public sealed record RouteReportRow(
    Guid RouteId,
    string Name,
    string Type,
    string Status,
    int DeliveryCount,
    decimal EstimatedKm,
    decimal? ActualKm,
    int? DurationMinutes
);

public sealed record RouteReport(
    List<RouteReportRow> Routes,
    int TotalRoutes,
    decimal TotalEstimatedKm
);

// ── Financial Report Responses ──

public sealed record CostSummaryByCategoryRow(
    string CategoryName,
    decimal TotalAmount,
    decimal Percentage,
    int TransactionCount
);

public sealed record CostSummaryReport(
    List<CostSummaryByCategoryRow> Categories,
    decimal GrandTotal,
    string Currency
);

public sealed record CostByVehicleRow(
    Guid VehicleId,
    string Plate,
    string BrandModel,
    decimal TotalFuel,
    decimal TotalMaintenance,
    decimal TotalExpenses,
    decimal GrandTotal,
    decimal CostPerKm
);

public sealed record CostByVehicleReport(
    List<CostByVehicleRow> Vehicles,
    decimal GrandTotal
);

public sealed record CostTrendRow(
    int Year,
    int Month,
    string MonthLabel,
    decimal FuelCost,
    decimal MaintenanceCost,
    decimal ExpenseCost,
    decimal TotalCost
);

public sealed record CostTrendReport(
    List<CostTrendRow> Trends,
    decimal GrandTotal
);

public sealed record ProfitabilityRow(
    string EntityName,
    decimal TotalCost,
    decimal? TotalRevenue,
    decimal? Margin
);

public sealed record ProfitabilityReport(
    List<ProfitabilityRow> Rows,
    decimal TotalCost,
    decimal? TotalRevenue,
    decimal? OverallMargin
);

// ── Managerial Report Responses ──

public sealed record FleetKpiReport(
    int TotalVehicles,
    int ActiveVehicles,
    int AvailableVehicles,
    int InMaintenanceVehicles,
    int OutOfServiceVehicles,
    decimal FleetAvailabilityRate,
    decimal AverageCostPerKm,
    decimal AverageFuelEfficiency,
    int TotalDeliveries,
    int CompletedDeliveries,
    decimal OnTimeDeliveryRate,
    int ExpiringDocuments,
    int OverdueMaintenance,
    int OpenWorkOrders
);

public sealed class FleetKpiScalars
{
    public int TotalVehicles { get; set; }
    public int ActiveVehicles { get; set; }
    public int AvailableVehicles { get; set; }
    public int InMaintenanceVehicles { get; set; }
    public int OutOfServiceVehicles { get; set; }
    public decimal FleetAvailabilityRate { get; set; }
    public decimal AverageCostPerKm { get; set; }
    public decimal AverageFuelEfficiency { get; set; }
    public int TotalDeliveries { get; set; }
    public int CompletedDeliveries { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public int ExpiringDocuments { get; set; }
    public int OverdueMaintenance { get; set; }
    public int OpenWorkOrders { get; set; }
}

public sealed record DriverScorecardRow(
    Guid DriverId,
    string FullName,
    string LicenseNumber,
    string Status,
    int TripCount,
    decimal TotalKm,
    decimal AverageFuelEfficiency,
    int DeliveryCount,
    int CompletedDeliveries,
    decimal OnTimeRate,
    decimal TotalExpenses
);

public sealed record DriverScorecardReport(
    List<DriverScorecardRow> Drivers,
    int TotalDrivers,
    int ActiveDrivers
);

public sealed record VehicleScorecardRow(
    Guid VehicleId,
    string Plate,
    string BrandModel,
    string Status,
    decimal CurrentKm,
    int TripCount,
    decimal TotalFuelLiters,
    decimal AverageFuelEfficiency,
    decimal TotalFuelCost,
    decimal TotalMaintenanceCost,
    decimal TotalExpenses,
    int WorkOrderCount,
    int OpenWorkOrders
);

public sealed record VehicleScorecardReport(
    List<VehicleScorecardRow> Vehicles,
    int TotalVehicles
);

public sealed record FuelTrendRow(
    int Year,
    int Month,
    string MonthLabel,
    decimal TotalLiters,
    decimal TotalCost,
    decimal AveragePricePerLiter,
    decimal AverageEfficiency
);

public sealed record FuelTrendReport(
    List<FuelTrendRow> Trends,
    decimal GrandTotalLiters,
    decimal GrandTotalCost
);

// ── Expense by Account Report (AI Classification) ──

public sealed record ExpenseByAccountRow(
    string AccountCode,
    string AccountName,
    decimal TotalAmount,
    decimal Percentage,
    int ExpenseCount,
    int ApprovedCount,
    int PendingCount
);

public sealed record ExpenseByAccountReport(
    List<ExpenseByAccountRow> Accounts,
    decimal GrandTotal,
    int TotalExpenses,
    int TotalApproved,
    int TotalPending
);

// ── Export ──

public sealed record FleetExportRequest(
    string ReportType,
    string Format, // "pdf" or "xlsx"
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? VehicleId,
    Guid? DriverId,
    Guid? CategoryId,
    string? Currency
);
