using Zorvian.Application.DTOs.Dashboard;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class DashboardService
{
    private const string PermissionType = "permiso";
    private const string VacationType = "vacacion";
    private readonly IDashboardRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly ISaleRepository _saleRepo;
    private readonly ICreditRepository _creditRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICashRegisterRepository _cashRepo;

    public DashboardService(
        IDashboardRepository repo,
        ITenantContext tenant,
        ISaleRepository saleRepo,
        ICreditRepository creditRepo,
        IProductRepository productRepo,
        ICashRegisterRepository cashRepo)
    {
        _repo = repo;
        _tenant = tenant;
        _saleRepo = saleRepo;
        _creditRepo = creditRepo;
        _productRepo = productRepo;
        _cashRepo = cashRepo;
    }

    /// <summary>
    /// Loads ALL dashboard data with maximum parallelism.
    /// Uses a single raw SQL for all 11 scalar KPIs (1 round-trip).
    /// Department counts + calendar + recent requests run in parallel.
    /// Total: ~3 network round-trips instead of 16.
    /// </summary>
    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30));
        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;
        var start = new DateOnly(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        // Phase 1: Single raw SQL for all 11 scalar KPIs (1 round-trip)
        var scalars = await _repo.GetAllKpiScalarsRawAsync(
            _tenant.TenantId.Value.ToString(),
            _tenant.IsSuperAdmin,
            thirtyDaysAgo, now.Month, lastMonth);

        // Phase 2: EF Core queries in parallel (after raw SQL completes — safe for DbContext)
        var byDeptTask = _repo.GetEmployeesByDepartmentAsync();
        var calendarTask = _repo.GetVacationsInRangeAsync(start, end);
        var recentPermissionsTask = _repo.GetRecentPermissionsAsync(10);
        var recentVacationsTask = _repo.GetRecentVacationsAsync(10);

        await Task.WhenAll(byDeptTask, calendarTask, recentPermissionsTask, recentVacationsTask);

        var byDept = await byDeptTask;
        var vacations = await calendarTask;
        var permissions = await recentPermissionsTask;
        var recentVacations = await recentVacationsTask;

        // Build KPIs from single-query results
        var total = scalars.TotalEmployees;
        var active = scalars.ActiveEmployees;
        var inactive = total - active;

        double? activeTrend = scalars.PreviousMonthActiveEmployees > 0
            ? Math.Round(((double)(active - scalars.PreviousMonthActiveEmployees) / scalars.PreviousMonthActiveEmployees) * 100, 1)
            : null;

        var totalPending = scalars.PendingVacationRequests + scalars.PendingPermissionRequests;
        var prevPending = totalPending + scalars.ResolvedVacationCount + scalars.ResolvedPermissionCount;
        double? pendingTrend = prevPending > 0
            ? Math.Round(((double)(totalPending - prevPending) / prevPending) * 100, 1)
            : null;

        double? bdayTrend = scalars.PrevBirthdayCount > 0
            ? Math.Round(((double)(scalars.BirthdayCount - scalars.PrevBirthdayCount) / scalars.PrevBirthdayCount) * 100, 1)
            : null;
        double? annivTrend = scalars.PrevAnniversaryCount > 0
            ? Math.Round(((double)(scalars.AnniversaryCount - scalars.PrevAnniversaryCount) / scalars.PrevAnniversaryCount) * 100, 1)
            : null;

        var kpis = new DashboardKpisResponse(
            total, active, inactive,
            activeTrend, pendingTrend, bdayTrend, annivTrend,
            byDept.Select(d => new DepartmentCount(d.Name, d.Count)).ToList(),
            scalars.PendingVacationRequests, scalars.PendingPermissionRequests,
            scalars.BirthdayCount, scalars.AnniversaryCount);

        // Build calendar events
        var calendarEvents = vacations.Select(v => new VacationCalendarEvent(
            v.EmployeeId,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Employee.EmployeeCode ?? "",
            "vacation",
            v.StartDate, v.EndDate, v.Status
        )).ToList();

        // Build recent requests
        var recentRequests = new List<RecentRequestItem>();
        recentRequests.AddRange(permissions.Select(p => new RecentRequestItem(
            p.Id, PermissionType,
            $"{p.Employee.FirstName} {p.Employee.LastName}",
            p.Status, p.LeaveType?.Name, p.CreatedAt)));
        recentRequests.AddRange(recentVacations.Select(v => new RecentRequestItem(
            v.Id, VacationType,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Status, $"{v.StartDate} - {v.EndDate}", v.CreatedAt)));
        recentRequests = recentRequests.OrderByDescending(r => r.CreatedAt).Take(10).ToList();

        return new DashboardSummaryResponse(kpis, calendarEvents, recentRequests);
    }

    public async Task<DashboardKpisResponse> GetKpisAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30));
        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;

        // Phase 1: Raw SQL for all 11 scalar KPIs (1 round-trip)
        var scalars = await _repo.GetAllKpiScalarsRawAsync(
            _tenant.TenantId.Value.ToString(),
            _tenant.IsSuperAdmin,
            thirtyDaysAgo, now.Month, lastMonth);

        // Phase 2: Department query (after raw SQL — safe for DbContext)
        var byDept = await _repo.GetEmployeesByDepartmentAsync();

        var total = scalars.TotalEmployees;
        var active = scalars.ActiveEmployees;
        var inactive = total - active;

        double? activeTrend = scalars.PreviousMonthActiveEmployees > 0
            ? Math.Round(((double)(active - scalars.PreviousMonthActiveEmployees) / scalars.PreviousMonthActiveEmployees) * 100, 1)
            : null;

        var totalPending = scalars.PendingVacationRequests + scalars.PendingPermissionRequests;
        var prevPending = totalPending + scalars.ResolvedVacationCount + scalars.ResolvedPermissionCount;
        double? pendingTrend = prevPending > 0
            ? Math.Round(((double)(totalPending - prevPending) / prevPending) * 100, 1)
            : null;

        double? bdayTrend = scalars.PrevBirthdayCount > 0
            ? Math.Round(((double)(scalars.BirthdayCount - scalars.PrevBirthdayCount) / scalars.PrevBirthdayCount) * 100, 1)
            : null;
        double? annivTrend = scalars.PrevAnniversaryCount > 0
            ? Math.Round(((double)(scalars.AnniversaryCount - scalars.PrevAnniversaryCount) / scalars.PrevAnniversaryCount) * 100, 1)
            : null;

        return new DashboardKpisResponse(
            total, active, inactive,
            activeTrend, pendingTrend, bdayTrend, annivTrend,
            byDept.Select(d => new DepartmentCount(d.Name, d.Count)).ToList(),
            scalars.PendingVacationRequests, scalars.PendingPermissionRequests,
            scalars.BirthdayCount, scalars.AnniversaryCount);
    }

    /// <summary>
    /// Executive dashboard: 19 queries across 5 repositories.
    /// All queries are independent → fire all in parallel (1 round-trip).
    /// Each repo uses its own DbContext connection from the pool.
    /// </summary>
    public async Task<ExecutiveDashboardResponse> GetExecutiveDashboardAsync()
    {
        // Commercial KPIs (saleRepo)
        var todaySalesTask = _saleRepo.GetTodaySalesAsync(Guid.Empty);
        var monthSalesTask = _saleRepo.GetMonthSalesAsync(Guid.Empty);
        var avgTicketTask = _saleRepo.GetAverageTicketAsync(Guid.Empty);
        var todaySalesCountTask = _saleRepo.GetTodaySalesCountAsync(Guid.Empty);

        // Credit KPIs (creditRepo)
        var activeCreditsTask = _creditRepo.GetActiveCreditsCountAsync(Guid.Empty);
        var overdueCreditsTask = _creditRepo.GetOverdueCreditsCountAsync(Guid.Empty);
        var monthlyRecoveryTask = _creditRepo.GetMonthlyRecoveryAsync(Guid.Empty);
        var totalPortfolioTask = _creditRepo.GetTotalPortfolioAsync(Guid.Empty);

        // Inventory KPIs (productRepo)
        var outOfStockTask = _productRepo.GetOutOfStockAsync(Guid.Empty);
        var lowStockTask = _productRepo.GetLowStockAsync(Guid.Empty);
        var totalProductsTask = _productRepo.GetTotalCountAsync(Guid.Empty);
        var topSellingTask = _productRepo.GetTopSellingAsync(Guid.Empty, 5);

        // Cash KPIs (cashRepo)
        var todayIncomeTask = _cashRepo.GetTodayIncomeAsync(Guid.Empty);
        var todayExpenseTask = _cashRepo.GetTodayExpenseAsync(Guid.Empty);
        var openRegistersTask = _cashRepo.GetOpenRegistersCountAsync(Guid.Empty);

        // HR KPIs (dashRepo)
        var activeEmployeesTask = _repo.GetActiveEmployeesAsync();
        var totalEmployeesTask = _repo.GetTotalEmployeesAsync();
        var pendingVacTask = _repo.GetPendingVacationRequestsAsync();
        var pendingPermTask = _repo.GetPendingPermissionRequestsAsync();

        // Fire all 19 queries in parallel
        await Task.WhenAll(
            todaySalesTask, monthSalesTask, avgTicketTask, todaySalesCountTask,
            activeCreditsTask, overdueCreditsTask, monthlyRecoveryTask, totalPortfolioTask,
            outOfStockTask, lowStockTask, totalProductsTask, topSellingTask,
            todayIncomeTask, todayExpenseTask, openRegistersTask,
            activeEmployeesTask, totalEmployeesTask, pendingVacTask, pendingPermTask);

        return new ExecutiveDashboardResponse(
            new CommercialKpis(await todaySalesTask, await monthSalesTask, await avgTicketTask, await todaySalesCountTask),
            new CreditKpis(await activeCreditsTask, await overdueCreditsTask, await monthlyRecoveryTask, await totalPortfolioTask),
            new InventoryKpis((await outOfStockTask).Count, (await lowStockTask).Count, await totalProductsTask,
                (await topSellingTask).Select(p => new TopProductItem(p.Product.Name, p.TotalSold)).ToList()),
            new CashKpis(await todayIncomeTask, await todayExpenseTask, await openRegistersTask),
            new HrKpis(await activeEmployeesTask, await pendingVacTask, await pendingPermTask, await totalEmployeesTask));
    }

    public async Task<List<VacationCalendarEvent>> GetVacationCalendarAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateOnly(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var vacations = await _repo.GetVacationsInRangeAsync(start, end);

        return vacations.Select(v => new VacationCalendarEvent(
            v.EmployeeId,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Employee.EmployeeCode ?? "",
            "vacation",
            v.StartDate,
            v.EndDate,
            v.Status
        )).ToList();
    }

    public async Task<List<RecentRequestItem>> GetRecentRequestsAsync(int count = 10)
    {
        // Parallel: permissions + vacations in one batch
        var permissionsTask = _repo.GetRecentPermissionsAsync(count);
        var vacationsTask = _repo.GetRecentVacationsAsync(count);
        await Task.WhenAll(permissionsTask, vacationsTask);

        var permissions = await permissionsTask;
        var vacations = await vacationsTask;

        var result = new List<RecentRequestItem>();
        result.AddRange(permissions.Select(p => new RecentRequestItem(
            p.Id, PermissionType,
            $"{p.Employee.FirstName} {p.Employee.LastName}",
            p.Status, p.LeaveType?.Name, p.CreatedAt)));
        result.AddRange(vacations.Select(v => new RecentRequestItem(
            v.Id, VacationType,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Status, $"{v.StartDate} - {v.EndDate}", v.CreatedAt)));

        return result.OrderByDescending(r => r.CreatedAt).Take(count).ToList();
    }

    public async Task<PayrollDashboardResponse> GetPayrollDashboardAsync()
    {
        var costs = await _repo.GetPayrollCostByDepartmentAsync();
        var history = await _repo.GetPayrollHistoryAsync(6);

        return new PayrollDashboardResponse(
            costs.Select(c => new PayrollCostByDept(c.Department, c.Amount)).ToList(),
            history.Select(h => new PayrollHistoryItem(h.Period, h.Amount)).ToList(),
            costs.Sum(c => c.Amount)
        );
    }
}
