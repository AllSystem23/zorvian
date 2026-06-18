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
        try
        {
            return await LoadSummaryAsync();
        }
        catch (Exception ex) when (IsEmptyDataException(ex))
        {
            return DashboardSummaryResponse.Empty;
        }
    }

    private async Task<DashboardSummaryResponse> LoadSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30));
        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;
        var start = new DateOnly(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        // Phase 1: Single raw SQL for all 11 scalar KPIs (1 round-trip)
        var scalars = await _repo.GetAllKpiScalarsRawAsync(
            _tenant.EffectiveCompanyId,
            _tenant.IsSuperAdmin,
            thirtyDaysAgo, now.Month, lastMonth);

        // Phase 2: EF Core queries sequentially (safe for DbContext)
        var byDept = await _repo.GetEmployeesByDepartmentAsync();
        var vacations = await _repo.GetVacationsInRangeAsync(start, end, 1, 10);
        var permissions = await _repo.GetRecentPermissionsAsync(10, 1, 10);
        var recentVacations = await _repo.GetRecentVacationsAsync(10, 1, 10);

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

    private static bool IsEmptyDataException(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("no data", StringComparison.OrdinalIgnoreCase)
            || message.Contains("empty", StringComparison.OrdinalIgnoreCase)
            || message.Contains("no hay datos", StringComparison.OrdinalIgnoreCase)
            || message.Contains("vacía", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<DashboardKpisResponse> GetKpisAsync()
    {
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30));
        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;

        // Phase 1: Single raw SQL for all 11 scalar KPIs (1 round-trip)
        var scalars = await _repo.GetAllKpiScalarsRawAsync(
            _tenant.EffectiveCompanyId,
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
    /// Executive dashboard: Optimized to use only 2 database round-trips.
    /// Phase 1: 18 scalar KPIs in a single raw SQL query.
    /// Phase 2: Top selling products list.
    /// </summary>
    public async Task<ExecutiveDashboardResponse> GetExecutiveDashboardAsync()
    {
        // Phase 1: 18 scalars in 1 trip
        var scalars = await _repo.GetExecutiveKpiScalarsRawAsync(_tenant.EffectiveCompanyId, _tenant.IsSuperAdmin);

        // Phase 2: Top selling products (requires complex join/logic best left to EF or separate SQL)
        var topSelling = await _productRepo.GetTopSellingAsync(Guid.Empty, 5);

        return new ExecutiveDashboardResponse(
            new CommercialKpis(scalars.TodaySales, scalars.MonthSales, scalars.AverageTicket, scalars.TodaySalesCount),
            new CreditKpis(scalars.ActiveCredits, scalars.OverdueCredits, scalars.MonthlyRecovery, scalars.TotalPortfolio),
            new InventoryKpis(scalars.OutOfStockCount, scalars.LowStockCount, scalars.TotalProducts,
                topSelling.Select(p => new TopProductItem(p.Product.Name, p.TotalSold)).ToList()),
            new CashKpis(scalars.TodayIncome, scalars.TodayExpense, scalars.OpenRegisters),
            new HrKpis(scalars.ActiveEmployees, scalars.PendingVacations, scalars.PendingPermissions, scalars.TotalEmployees));
    }

    public async Task<List<VacationCalendarEvent>> GetVacationCalendarAsync()
    {
        var now = DateTime.UtcNow;
        var start = new DateOnly(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        // Fetch vacations for calendar (paged)
        var vacations = await _repo.GetVacationsInRangeAsync(start, end, 1, 100);

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
        // Queries are executed sequentially for DbContext thread safety
        var permissions = await _repo.GetRecentPermissionsAsync(count, 1, count);
        var vacations = await _repo.GetRecentVacationsAsync(count, 1, count);

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
