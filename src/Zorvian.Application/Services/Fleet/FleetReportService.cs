using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetReportService
{
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IDriverRepository _driverRepo;
    private readonly IRouteRepository _routeRepo;
    private readonly IDeliveryRepository _deliveryRepo;
    private readonly ITripRepository _tripRepo;
    private readonly IFuelRefillRepository _fuelRepo;
    private readonly IFleetExpenseRepository _expenseRepo;
    private readonly IWorkOrderRepository _workOrderRepo;
    private readonly IFleetDocumentRepository _documentRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ITenantContext _tenant;

    public FleetReportService(
        IVehicleRepository vehicleRepo,
        IDriverRepository driverRepo,
        IRouteRepository routeRepo,
        IDeliveryRepository deliveryRepo,
        ITripRepository tripRepo,
        IFuelRefillRepository fuelRepo,
        IFleetExpenseRepository expenseRepo,
        IWorkOrderRepository workOrderRepo,
        IFleetDocumentRepository documentRepo,
        IAccountRepository accountRepo,
        ITenantContext tenant)
    {
        _vehicleRepo = vehicleRepo;
        _driverRepo = driverRepo;
        _routeRepo = routeRepo;
        _deliveryRepo = deliveryRepo;
        _tripRepo = tripRepo;
        _fuelRepo = fuelRepo;
        _expenseRepo = expenseRepo;
        _workOrderRepo = workOrderRepo;
        _documentRepo = documentRepo;
        _accountRepo = accountRepo;
        _tenant = tenant;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            throw new InvalidOperationException("Tenant not configured");
        return companyId;
    }

    // ════════════════════════════════════════
    //  OPERATIONAL REPORTS
    // ════════════════════════════════════════

    public async Task<VehicleUsageReport> GetVehicleUsageAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var trips = await _tripRepo.GetAllAsync();

        if (request.VehicleId.HasValue)
            vehicles = vehicles.Where(v => v.Id == request.VehicleId.Value).ToList();

        if (request.StartDate.HasValue)
            trips = trips.Where(t => t.StartDateTime >= request.StartDate.Value).ToList();
        if (request.EndDate.HasValue)
            trips = trips.Where(t => t.StartDateTime <= request.EndDate.Value).ToList();

        var vehicleTrips = trips.GroupBy(t => t.VehicleId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = vehicles.Select(v =>
        {
            var vTrips = vehicleTrips.GetValueOrDefault(v.Id, []);
            var totalKm = vTrips.Sum(t => t.TotalKm ?? 0);
            var totalHours = vTrips.Where(t => t.DurationMinutes.HasValue).Sum(t => t.DurationMinutes!.Value) / 60.0m;
            var tripCount = vTrips.Count;
            var avgKm = tripCount > 0 ? totalKm / tripCount : 0;

            return new VehicleUsageReportRow(
                v.Id, v.Plate, $"{v.Brand.Name} {v.Model}", v.Status,
                totalKm, tripCount, Math.Round(totalHours, 1), Math.Round(avgKm, 1));
        }).OrderByDescending(r => r.TotalKm).ToList();

        return new VehicleUsageReport(
            rows,
            rows.Sum(r => r.TotalKm),
            rows.Sum(r => r.TripCount),
            rows.Sum(r => r.TotalHours));
    }

    public async Task<DeliveryReport> GetDeliveryReportAsync(FleetReportRequest request)
    {
        var deliveries = await _deliveryRepo.GetAllAsync();

        if (request.StartDate.HasValue)
            deliveries = deliveries.Where(d => d.ScheduledDate >= DateOnly.FromDateTime(request.StartDate.Value)).ToList();
        if (request.EndDate.HasValue)
            deliveries = deliveries.Where(d => d.ScheduledDate <= DateOnly.FromDateTime(request.EndDate.Value)).ToList();
        if (request.VehicleId.HasValue)
            deliveries = deliveries.Where(d => d.VehicleId == request.VehicleId).ToList();
        if (request.DriverId.HasValue)
            deliveries = deliveries.Where(d => d.DriverId == request.DriverId).ToList();

        var total = deliveries.Count;
        var completed = deliveries.Count(d => d.Status == "Delivered");
        var onTime = deliveries.Count(d => d.Status == "Delivered" && d.DeliveredAt.HasValue &&
            d.DeliveredAt.Value <= d.ScheduledDate.ToDateTime(TimeOnly.MinValue).AddDays(1));

        var byStatus = deliveries
            .GroupBy(d => d.Status)
            .Select(g => new DeliveryReportRow(g.Key, g.Count(), total > 0 ? Math.Round((decimal)g.Count() / total * 100, 1) : 0))
            .OrderByDescending(r => r.Count)
            .ToList();

        return new DeliveryReport(byStatus, total, onTime, total > 0 ? Math.Round((decimal)onTime / total * 100, 1) : 0);
    }

    public async Task<RouteReport> GetRouteReportAsync(FleetReportRequest request)
    {
        var routes = await _routeRepo.GetAllAsync();
        var deliveries = await _deliveryRepo.GetAllAsync();

        if (request.StartDate.HasValue)
            routes = routes.Where(r => r.CreatedAt >= request.StartDate.Value).ToList();
        if (request.EndDate.HasValue)
            routes = routes.Where(r => r.CreatedAt <= request.EndDate.Value).ToList();

        var deliveryCounts = deliveries.GroupBy(d => d.RouteId).ToDictionary(g => g.Key, g => g.Count());

        var rows = routes.Select(r => new RouteReportRow(
            r.Id, r.Name, r.Type, r.Status,
            deliveryCounts.GetValueOrDefault(r.Id, 0),
            r.DistanceEstKm,
            null,
            r.DurationEstMinutes)).ToList();

        return new RouteReport(rows, rows.Count, rows.Sum(r => r.EstimatedKm));
    }

    // ════════════════════════════════════════
    //  FINANCIAL REPORTS
    // ════════════════════════════════════════

    public async Task<CostSummaryReport> GetCostSummaryAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var expenses = await _expenseRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);
        var workOrders = await _workOrderRepo.GetAllAsync(companyId);

        if (request.StartDate.HasValue)
        {
            expenses = expenses.Where(e => e.ExpenseDate >= request.StartDate.Value).ToList();
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= request.StartDate.Value).ToList();
            workOrders = workOrders.Where(w => w.CreatedAt >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            expenses = expenses.Where(e => e.ExpenseDate <= request.EndDate.Value).ToList();
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime <= request.EndDate.Value).ToList();
            workOrders = workOrders.Where(w => w.CreatedAt <= request.EndDate.Value).ToList();
        }

        var fuelTotal = fuelRefills.Sum(f => f.TotalCost);
        var expenseByCategory = expenses
            .GroupBy(e => e.Category?.Name ?? "Sin categoría")
            .Select(g => new CostSummaryByCategoryRow(g.Key, g.Sum(e => e.Amount), 0, g.Count()))
            .ToList();

        var maintenanceCost = workOrders.Sum(w => w.CostTotal);
        if (maintenanceCost > 0)
            expenseByCategory.Add(new CostSummaryByCategoryRow("Mantenimiento / Taller", maintenanceCost, 0, workOrders.Count));

        if (fuelTotal > 0)
            expenseByCategory.Insert(0, new CostSummaryByCategoryRow("Combustible", fuelTotal, 0, fuelRefills.Count));

        var grandTotal = expenseByCategory.Sum(c => c.TotalAmount);
        var withPercentage = expenseByCategory
            .Select(c => c with { Percentage = grandTotal > 0 ? Math.Round(c.TotalAmount / grandTotal * 100, 1) : 0 })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        return new CostSummaryReport(withPercentage, grandTotal, "NIO");
    }

    public async Task<CostByVehicleReport> GetCostByVehicleAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);
        var expenses = await _expenseRepo.GetAllAsync(companyId);

        if (request.StartDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= request.StartDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime <= request.EndDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate <= request.EndDate.Value).ToList();
        }
        if (request.VehicleId.HasValue)
            vehicles = vehicles.Where(v => v.Id == request.VehicleId.Value).ToList();

        var fuelByVehicle = fuelRefills.GroupBy(f => f.VehicleId).ToDictionary(g => g.Key, g => g.Sum(f => f.TotalCost));
        var expByVehicle = expenses.Where(e => e.VehicleId.HasValue)
            .GroupBy(e => e.VehicleId!.Value).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var rows = vehicles.Select(v =>
        {
            var fuelCost = fuelByVehicle.GetValueOrDefault(v.Id, 0);
            var expCost = expByVehicle.GetValueOrDefault(v.Id, 0);
            var total = fuelCost + expCost;
            var kmDiff = v.CurrentKm - v.PreviousKm;
            var costPerKm = kmDiff > 0 ? Math.Round(total / kmDiff, 2) : 0;

            return new CostByVehicleRow(
                v.Id, v.Plate, $"{v.Brand.Name} {v.Model}",
                fuelCost, 0, expCost, total, costPerKm);
        }).OrderByDescending(r => r.GrandTotal).ToList();

        return new CostByVehicleReport(rows, rows.Sum(r => r.GrandTotal));
    }

    public async Task<CostTrendReport> GetCostTrendAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var expenses = await _expenseRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-12);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        expenses = expenses.Where(e => e.ExpenseDate >= startDate && e.ExpenseDate <= endDate).ToList();
        fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= startDate && f.RefillDateTime <= endDate).ToList();

        var months = new List<(int Year, int Month)>();
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        while (current <= endDate)
        {
            months.Add((current.Year, current.Month));
            current = current.AddMonths(1);
        }

        var fuelByMonth = fuelRefills.GroupBy(f => (f.RefillDateTime.Year, f.RefillDateTime.Month))
            .ToDictionary(g => g.Key, g => g.Sum(f => f.TotalCost));
        var expByMonth = expenses.GroupBy(e => (e.ExpenseDate.Year, e.ExpenseDate.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var monthNames = new[] { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        var trends = months.Select(m =>
        {
            var fuel = fuelByMonth.GetValueOrDefault(m, 0);
            var exp = expByMonth.GetValueOrDefault(m, 0);
            return new CostTrendRow(m.Year, m.Month, $"{monthNames[m.Month]} {m.Year}", fuel, 0, exp, fuel + exp);
        }).ToList();

        return new CostTrendReport(trends, trends.Sum(t => t.TotalCost));
    }

    public async Task<ProfitabilityReport> GetProfitabilityAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);
        var expenses = await _expenseRepo.GetAllAsync(companyId);

        if (request.StartDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= request.StartDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime <= request.EndDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate <= request.EndDate.Value).ToList();
        }

        var fuelByVehicle = fuelRefills.GroupBy(f => f.VehicleId).ToDictionary(g => g.Key, g => g.Sum(f => f.TotalCost));
        var expByVehicle = expenses.Where(e => e.VehicleId.HasValue)
            .GroupBy(e => e.VehicleId!.Value).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var rows = vehicles.Select(v =>
        {
            var cost = fuelByVehicle.GetValueOrDefault(v.Id, 0) + expByVehicle.GetValueOrDefault(v.Id, 0);
            return new ProfitabilityRow($"{v.Plate} — {v.Brand.Name} {v.Model}", cost, null, null);
        }).Where(r => r.TotalCost > 0).OrderByDescending(r => r.TotalCost).ToList();

        var totalCost = rows.Sum(r => r.TotalCost);
        return new ProfitabilityReport(rows, totalCost, null, null);
    }

    // ════════════════════════════════════════
    //  MANAGERIAL REPORTS
    // ════════════════════════════════════════

    public async Task<FleetKpiReport> GetFleetKpisAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var deliveries = await _deliveryRepo.GetAllAsync();
        var workOrders = await _workOrderRepo.GetAllAsync(companyId);
        var documents = await _documentRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);

        var total = vehicles.Count;
        var active = vehicles.Count(v => v.Status == "Active");
        var available = vehicles.Count(v => v.Status == "Available");
        var inMaint = vehicles.Count(v => v.Status == "Maintenance");
        var outOfService = vehicles.Count(v => v.Status == "OutOfService");

        var totalKm = fuelRefills.Where(f => f.ValidForCalculation).Sum(f => f.CurrentKm);
        var totalLiters = fuelRefills.Where(f => f.ValidForCalculation).Sum(f => f.Liters);
        var avgEfficiency = totalLiters > 0 ? Math.Round(totalKm / totalLiters, 2) : 0;

        var totalFuelCost = fuelRefills.Sum(f => f.TotalCost);
        var avgCostPerKm = totalKm > 0 ? Math.Round(totalFuelCost / totalKm, 2) : 0;

        var completed = deliveries.Count(d => d.Status == "Delivered");
        var totalDeliveries = deliveries.Count;
        var onTime = deliveries.Count(d => d.Status == "Delivered" && d.DeliveredAt.HasValue &&
            d.DeliveredAt.Value <= d.ScheduledDate.ToDateTime(TimeOnly.MinValue).AddDays(1));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expiringSoon = documents.Count(d => d.ExpiryDate != null && d.ExpiryDate.Value <= today.AddDays(30) && d.Status == "Valid");
        var overdueMaint = vehicles.Count(v => v.Status == "Maintenance");
        var openWO = workOrders.Count(w => w.Status != "Closed" && w.Status != "Cancelled");

        return new FleetKpiReport(
            total, active, available, inMaint, outOfService,
            total > 0 ? Math.Round((decimal)active / total * 100, 1) : 0,
            avgCostPerKm, avgEfficiency,
            totalDeliveries, completed,
            totalDeliveries > 0 ? Math.Round((decimal)onTime / totalDeliveries * 100, 1) : 0,
            expiringSoon, overdueMaint, openWO);
    }

    public async Task<DriverScorecardReport> GetDriverScorecardAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var drivers = await _driverRepo.GetAllAsync(companyId);
        var trips = await _tripRepo.GetAllAsync();
        var deliveries = await _deliveryRepo.GetAllAsync();
        var expenses = await _expenseRepo.GetAllAsync(companyId);

        if (request.StartDate.HasValue)
        {
            trips = trips.Where(t => t.StartDateTime >= request.StartDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            trips = trips.Where(t => t.StartDateTime <= request.EndDate.Value).ToList();
            expenses = expenses.Where(e => e.ExpenseDate <= request.EndDate.Value).ToList();
        }

        var tripsByDriver = trips.GroupBy(t => t.DriverId).ToDictionary(g => g.Key, g => g.ToList());
        var delByDriver = deliveries.GroupBy(d => d.DriverId).ToDictionary(g => g.Key, g => g.ToList());
        var expByDriver = expenses.Where(e => e.DriverId.HasValue)
            .GroupBy(e => e.DriverId!.Value).ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var rows = drivers.Select(d =>
        {
            var dTrips = tripsByDriver.GetValueOrDefault(d.Id, []);
            var dDeliveries = delByDriver.GetValueOrDefault(d.Id, []);
            var totalKm = dTrips.Sum(t => t.TotalKm ?? 0);
            var avgEff = dTrips.Count > 0 ? Math.Round(totalKm / dTrips.Count, 1) : 0;
            var completed = dDeliveries.Count(dd => dd.Status == "Delivered");
            var onTime = dDeliveries.Count(dd => dd.Status == "Delivered" && dd.DeliveredAt.HasValue &&
                dd.DeliveredAt.Value <= dd.ScheduledDate.ToDateTime(TimeOnly.MinValue).AddDays(1));

            return new DriverScorecardRow(
                d.Id, d.FullName, d.LicenseNumber, d.Status,
                dTrips.Count, totalKm, avgEff,
                dDeliveries.Count, completed,
                dDeliveries.Count > 0 ? Math.Round((decimal)onTime / dDeliveries.Count * 100, 1) : 0,
                expByDriver.GetValueOrDefault(d.Id, 0));
        }).OrderByDescending(r => r.TripCount).ToList();

        return new DriverScorecardReport(rows, rows.Count, rows.Count(r => r.Status == "Active"));
    }

    public async Task<VehicleScorecardReport> GetVehicleScorecardAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var vehicles = await _vehicleRepo.GetAllAsync(companyId);
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);
        var workOrders = await _workOrderRepo.GetAllAsync(companyId);

        if (request.StartDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= request.StartDate.Value).ToList();
            workOrders = workOrders.Where(w => w.CreatedAt >= request.StartDate.Value).ToList();
        }
        if (request.EndDate.HasValue)
        {
            fuelRefills = fuelRefills.Where(f => f.RefillDateTime <= request.EndDate.Value).ToList();
            workOrders = workOrders.Where(w => w.CreatedAt <= request.EndDate.Value).ToList();
        }

        var fuelByVehicle = fuelRefills.GroupBy(f => f.VehicleId).ToDictionary(g => g.Key, g => g.ToList());
        var woByVehicle = workOrders.GroupBy(w => w.VehicleId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = vehicles.Select(v =>
        {
            var vFuel = fuelByVehicle.GetValueOrDefault(v.Id, []);
            var vWo = woByVehicle.GetValueOrDefault(v.Id, []);
            var totalLiters = vFuel.Sum(f => f.Liters);
            var totalFuelCost = vFuel.Sum(f => f.TotalCost);
            var totalMaintCost = vWo.Sum(w => w.CostTotal);
            var avgEff = totalLiters > 0 ? Math.Round(vFuel.Where(f => f.ValidForCalculation).Sum(f => f.CurrentKm) / totalLiters, 2) : 0;

            return new VehicleScorecardRow(
                v.Id, v.Plate, $"{v.Brand.Name} {v.Model}", v.Status, v.CurrentKm,
                vFuel.Count, totalLiters, avgEff,
                totalFuelCost, totalMaintCost, totalFuelCost + totalMaintCost,
                vWo.Count, vWo.Count(w => w.Status != "Closed" && w.Status != "Cancelled"));
        }).OrderByDescending(r => r.TotalExpenses).ToList();

        return new VehicleScorecardReport(rows, rows.Count);
    }

    public async Task<FuelTrendReport> GetFuelTrendAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var fuelRefills = await _fuelRepo.GetAllAsync(companyId);

        var startDate = request.StartDate ?? DateTime.UtcNow.AddMonths(-12);
        var endDate = request.EndDate ?? DateTime.UtcNow;

        fuelRefills = fuelRefills.Where(f => f.RefillDateTime >= startDate && f.RefillDateTime <= endDate).ToList();

        var months = new List<(int Year, int Month)>();
        var current = new DateTime(startDate.Year, startDate.Month, 1);
        while (current <= endDate)
        {
            months.Add((current.Year, current.Month));
            current = current.AddMonths(1);
        }

        var byMonth = fuelRefills.GroupBy(f => (f.RefillDateTime.Year, f.RefillDateTime.Month))
            .ToDictionary(g => g.Key, g => g.ToList());

        var monthNames = new[] { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };

        var trends = months.Select(m =>
        {
            var mFuel = byMonth.GetValueOrDefault(m, []);
            var liters = mFuel.Sum(f => f.Liters);
            var cost = mFuel.Sum(f => f.TotalCost);
            var avgPrice = liters > 0 ? Math.Round(cost / liters, 4) : 0;
            var validKm = mFuel.Where(f => f.ValidForCalculation).Sum(f => f.CurrentKm);
            var avgEff = liters > 0 ? Math.Round(validKm / liters, 2) : 0;

            return new FuelTrendRow(m.Year, m.Month, $"{monthNames[m.Month]} {m.Year}", liters, cost, avgPrice, avgEff);
        }).ToList();

        return new FuelTrendReport(trends, trends.Sum(t => t.TotalLiters), trends.Sum(t => t.TotalCost));
    }

    // ════════════════════════════════════════
    //  EXPENSE BY ACCOUNT (AI Classification)
    // ════════════════════════════════════════

    public async Task<ExpenseByAccountReport> GetExpenseByAccountAsync(FleetReportRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        var expenses = await _expenseRepo.GetAllAsync(companyId);
        var accounts = await _accountRepo.GetAllAsync(companyId);
        var accountMap = accounts.ToDictionary(a => a.Id, a => (a.Code, a.Name));

        if (request.StartDate.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate >= request.StartDate.Value).ToList();
        if (request.EndDate.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate <= request.EndDate.Value).ToList();

        var byAccount = expenses
            .GroupBy(e => e.AccountId)
            .Select(g =>
            {
                var count = g.Count();
                var total = g.Sum(e => e.Amount);
                var approved = g.Count(e => e.Approved);
                var accId = g.Key;
                string code = "Sin cuenta";
                string name = "";
                if (accId.HasValue && accountMap.TryGetValue(accId.Value, out var found))
                {
                    code = found.Code;
                    name = found.Name;
                }
                var pct = 0m;
                return new ExpenseByAccountRow(code, name, total, pct, count, approved, count - approved);
            })
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var grandTotal = byAccount.Sum(r => r.TotalAmount);
        var totalExpenses = expenses.Count;
        var totalApproved = expenses.Count(e => e.Approved);

        var withPercentage = byAccount
            .Select(r => r with { Percentage = grandTotal > 0 ? Math.Round(r.TotalAmount / grandTotal * 100, 1) : 0 })
            .ToList();

        return new ExpenseByAccountReport(withPercentage, grandTotal, totalExpenses, totalApproved, totalExpenses - totalApproved);
    }

    // ════════════════════════════════════════
    //  EXPORT TO ReportResult
    // ════════════════════════════════════════

    public async Task<Application.DTOs.Report.ReportResult> GetReportAsResultAsync(FleetExportRequest request)
    {
        var reportRequest = new FleetReportRequest(
            request.StartDate, request.EndDate, request.VehicleId,
            request.DriverId, request.CategoryId, request.Currency);

        return request.ReportType switch
        {
            "vehicle-usage" => ToReportResult(await GetVehicleUsageAsync(reportRequest)),
            "delivery" => ToReportResult(await GetDeliveryReportAsync(reportRequest)),
            "route" => ToReportResult(await GetRouteReportAsync(reportRequest)),
            "cost-summary" => ToReportResult(await GetCostSummaryAsync(reportRequest)),
            "cost-by-vehicle" => ToReportResult(await GetCostByVehicleAsync(reportRequest)),
            "cost-trend" => ToReportResult(await GetCostTrendAsync(reportRequest)),
            "profitability" => ToReportResult(await GetProfitabilityAsync(reportRequest)),
            "fleet-kpi" => ToReportResult(await GetFleetKpisAsync()),
            "driver-scorecard" => ToReportResult(await GetDriverScorecardAsync(reportRequest)),
            "vehicle-scorecard" => ToReportResult(await GetVehicleScorecardAsync(reportRequest)),
            "fuel-trend" => ToReportResult(await GetFuelTrendAsync(reportRequest)),
            "expense-by-account" => ToReportResult(await GetExpenseByAccountAsync(reportRequest)),
            _ => throw new ArgumentException($"Unknown report type: {request.ReportType}")
        };
    }

    // ── Conversion helpers ──

    private static Application.DTOs.Report.ReportResult ToReportResult(VehicleUsageReport report)
    {
        var columns = new List<string> { "Placa", "Marca/Modelo", "Estado", "Km Totales", "Viajes", "Horas", "Km/Viaje" };
        var rows = report.Vehicles.Select(v => new Dictionary<string, object?>
        {
            ["Placa"] = v.Plate, ["Marca/Modelo"] = v.BrandModel, ["Estado"] = v.Status,
            ["Km Totales"] = v.TotalKm, ["Viajes"] = v.TripCount,
            ["Horas"] = v.TotalHours, ["Km/Viaje"] = v.AverageKmPerTrip
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(DeliveryReport report)
    {
        var columns = new List<string> { "Estado", "Cantidad", "Porcentaje (%)" };
        var rows = report.ByStatus.Select(r => new Dictionary<string, object?>
        {
            ["Estado"] = r.Status, ["Cantidad"] = r.Count, ["Porcentaje (%)"] = r.Percentage
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(RouteReport report)
    {
        var columns = new List<string> { "Nombre", "Tipo", "Estado", "Entregas", "Km Estimados" };
        var rows = report.Routes.Select(r => new Dictionary<string, object?>
        {
            ["Nombre"] = r.Name, ["Tipo"] = r.Type, ["Estado"] = r.Status,
            ["Entregas"] = r.DeliveryCount, ["Km Estimados"] = r.EstimatedKm
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(CostSummaryReport report)
    {
        var columns = new List<string> { "Categoría", "Monto Total", "Porcentaje (%)", "Transacciones" };
        var rows = report.Categories.Select(c => new Dictionary<string, object?>
        {
            ["Categoría"] = c.CategoryName, ["Monto Total"] = c.TotalAmount,
            ["Porcentaje (%)"] = c.Percentage, ["Transacciones"] = c.TransactionCount
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(CostByVehicleReport report)
    {
        var columns = new List<string> { "Placa", "Marca/Modelo", "Combustible", "Mantenimiento", "Gastos", "Total", "Costo/Km" };
        var rows = report.Vehicles.Select(v => new Dictionary<string, object?>
        {
            ["Placa"] = v.Plate, ["Marca/Modelo"] = v.BrandModel,
            ["Combustible"] = v.TotalFuel, ["Mantenimiento"] = v.TotalMaintenance,
            ["Gastos"] = v.TotalExpenses, ["Total"] = v.GrandTotal, ["Costo/Km"] = v.CostPerKm
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(CostTrendReport report)
    {
        var columns = new List<string> { "Período", "Combustible", "Mantenimiento", "Gastos", "Total" };
        var rows = report.Trends.Select(t => new Dictionary<string, object?>
        {
            ["Período"] = t.MonthLabel, ["Combustible"] = t.FuelCost,
            ["Mantenimiento"] = t.MaintenanceCost, ["Gastos"] = t.ExpenseCost, ["Total"] = t.TotalCost
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(ProfitabilityReport report)
    {
        var columns = new List<string> { "Vehículo", "Costo Total", "Ingresos", "Margen" };
        var rows = report.Rows.Select(r => new Dictionary<string, object?>
        {
            ["Vehículo"] = r.EntityName, ["Costo Total"] = r.TotalCost,
            ["Ingresos"] = r.TotalRevenue ?? 0, ["Margen"] = r.Margin ?? 0
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(FleetKpiReport kpis)
    {
        var columns = new List<string> { "KPI", "Valor" };
        var rows = new List<Dictionary<string, object?>>(14)
        {
            new() { ["KPI"] = "Total Vehículos", ["Valor"] = kpis.TotalVehicles },
            new() { ["KPI"] = "Vehículos Activos", ["Valor"] = kpis.ActiveVehicles },
            new() { ["KPI"] = "Disponibles", ["Valor"] = kpis.AvailableVehicles },
            new() { ["KPI"] = "En Mantenimiento", ["Valor"] = kpis.InMaintenanceVehicles },
            new() { ["KPI"] = "Fuera de Servicio", ["Valor"] = kpis.OutOfServiceVehicles },
            new() { ["KPI"] = "Disponibilidad (%)", ["Valor"] = kpis.FleetAvailabilityRate },
            new() { ["KPI"] = "Costo/Km Promedio", ["Valor"] = kpis.AverageCostPerKm },
            new() { ["KPI"] = "Eficiencia Combustible (km/L)", ["Valor"] = kpis.AverageFuelEfficiency },
            new() { ["KPI"] = "Total Entregas", ["Valor"] = kpis.TotalDeliveries },
            new() { ["KPI"] = "Entregas Completadas", ["Valor"] = kpis.CompletedDeliveries },
            new() { ["KPI"] = "Tasa Entrega a Tiempo (%)", ["Valor"] = kpis.OnTimeDeliveryRate },
            new() { ["KPI"] = "Docs por Vencer (30d)", ["Valor"] = kpis.ExpiringDocuments },
            new() { ["KPI"] = "Mantenimiento Vencido", ["Valor"] = kpis.OverdueMaintenance },
            new() { ["KPI"] = "OTs Abiertas", ["Valor"] = kpis.OpenWorkOrders }
        };
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(DriverScorecardReport report)
    {
        var columns = new List<string> { "Nombre", "Licencia", "Estado", "Viajes", "Km", "Entregas", "A Tiempo (%)", "Gastos" };
        var rows = report.Drivers.Select(d => new Dictionary<string, object?>
        {
            ["Nombre"] = d.FullName, ["Licencia"] = d.LicenseNumber, ["Estado"] = d.Status,
            ["Viajes"] = d.TripCount, ["Km"] = d.TotalKm, ["Entregas"] = d.CompletedDeliveries,
            ["A Tiempo (%)"] = d.OnTimeRate, ["Gastos"] = d.TotalExpenses
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(VehicleScorecardReport report)
    {
        var columns = new List<string> { "Placa", "Marca/Modelo", "Estado", "Km", "Combustible (L)", "Eficiencia (km/L)", "Costo Combustible", "Costo Mantenimiento", "Total" };
        var rows = report.Vehicles.Select(v => new Dictionary<string, object?>
        {
            ["Placa"] = v.Plate, ["Marca/Modelo"] = v.BrandModel, ["Estado"] = v.Status,
            ["Km"] = v.CurrentKm, ["Combustible (L)"] = v.TotalFuelLiters,
            ["Eficiencia (km/L)"] = v.AverageFuelEfficiency,
            ["Costo Combustible"] = v.TotalFuelCost, ["Costo Mantenimiento"] = v.TotalMaintenanceCost,
            ["Total"] = v.TotalExpenses
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(FuelTrendReport report)
    {
        var columns = new List<string> { "Período", "Litros", "Costo Total", "Precio/L", "Eficiencia (km/L)" };
        var rows = report.Trends.Select(t => new Dictionary<string, object?>
        {
            ["Período"] = t.MonthLabel, ["Litros"] = t.TotalLiters,
            ["Costo Total"] = t.TotalCost, ["Precio/L"] = t.AveragePricePerLiter,
            ["Eficiencia (km/L)"] = t.AverageEfficiency
        }).ToList();
        return new(columns, rows, rows.Count);
    }

    private static Application.DTOs.Report.ReportResult ToReportResult(ExpenseByAccountReport report)
    {
        var columns = new List<string> { "Cuenta Contable", "Nombre", "Monto Total", "Porcentaje (%)", "Gastos", "Aprobados", "Pendientes" };
        var rows = report.Accounts.Select(a => new Dictionary<string, object?>
        {
            ["Cuenta Contable"] = a.AccountCode, ["Nombre"] = a.AccountName,
            ["Monto Total"] = a.TotalAmount, ["Porcentaje (%)"] = a.Percentage,
            ["Gastos"] = a.ExpenseCount, ["Aprobados"] = a.ApprovedCount,
            ["Pendientes"] = a.PendingCount
        }).ToList();
        return new(columns, rows, rows.Count);
    }
}
