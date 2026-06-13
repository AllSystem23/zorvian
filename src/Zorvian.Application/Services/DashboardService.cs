using Zorvian.Application.DTOs.Dashboard;
using Zorvian.Application.Interfaces;

namespace Zorvian.Application.Services;

public sealed class DashboardService
{
    private const string PermissionType = "permiso";
    private const string VacationType = "vacacion";
    private readonly IDashboardRepository _repo;
    private readonly ISaleRepository _saleRepo;
    private readonly ICreditRepository _creditRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICashRegisterRepository _cashRepo;

    public DashboardService(
        IDashboardRepository repo,
        ISaleRepository saleRepo,
        ICreditRepository creditRepo,
        IProductRepository productRepo,
        ICashRegisterRepository cashRepo)
    {
        _repo = repo;
        _saleRepo = saleRepo;
        _creditRepo = creditRepo;
        _productRepo = productRepo;
        _cashRepo = cashRepo;
    }

    public async Task<DashboardSummaryResponse> GetSummaryAsync()
    {
        var kpis = await GetKpisAsync();
        var calendar = await GetVacationCalendarAsync();
        var requests = await GetRecentRequestsAsync();

        return new DashboardSummaryResponse(kpis, calendar, requests);
    }

    public async Task<DashboardKpisResponse> GetKpisAsync()
    {
        var total = await _repo.GetTotalEmployeesAsync();
        var active = await _repo.GetActiveEmployeesAsync();
        var inactive = await _repo.GetInactiveEmployeesAsync();
        var byDept = await _repo.GetEmployeesByDepartmentAsync();
        var pendingVac = await _repo.GetPendingVacationRequestsAsync();
        var pendingPerm = await _repo.GetPendingPermissionRequestsAsync();
        var birthdays = await _repo.GetEmployeesWithBirthdayThisMonthAsync();
        var anniversaries = await _repo.GetEmployeesWithAnniversaryThisMonthAsync();
        var prevActive = await _repo.GetPreviousMonthActiveEmployeesAsync();

        double? activeTrend = prevActive > 0
            ? Math.Round(((double)(active - prevActive) / prevActive) * 100, 1)
            : null;

        var now = DateTime.UtcNow;
        var thirtyDaysAgo = DateOnly.FromDateTime(now.AddDays(-30));
        var resolvedVac = await _repo.GetResolvedVacationCountSinceAsync(thirtyDaysAgo);
        var resolvedPerm = await _repo.GetResolvedPermissionCountSinceAsync(thirtyDaysAgo);
        var totalPending = pendingVac + pendingPerm;
        var prevPending = totalPending + resolvedVac + resolvedPerm;
        double? pendingTrend = prevPending > 0
            ? Math.Round(((double)(totalPending - prevPending) / prevPending) * 100, 1)
            : null;

        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;
        var prevBirthdays = await _repo.GetEmployeesWithBirthdayInMonthAsync(lastMonth);
        var prevAnniversaries = await _repo.GetEmployeesWithAnniversaryInMonthAsync(lastMonth);
        double? bdayTrend = prevBirthdays.Count > 0
            ? Math.Round(((double)(birthdays.Count - prevBirthdays.Count) / prevBirthdays.Count) * 100, 1)
            : null;
        double? annivTrend = prevAnniversaries.Count > 0
            ? Math.Round(((double)(anniversaries.Count - prevAnniversaries.Count) / prevAnniversaries.Count) * 100, 1)
            : null;

        return new DashboardKpisResponse(
            total,
            active,
            inactive,
            activeTrend,
            pendingTrend,
            bdayTrend,
            annivTrend,
            byDept.Select(d => new DepartmentCount(d.Name, d.Count)).ToList(),
            pendingVac,
            pendingPerm,
            birthdays.Count,
            anniversaries.Count
        );
    }

    public async Task<ExecutiveDashboardResponse> GetExecutiveDashboardAsync()
    {
        var todaySales = await _saleRepo.GetTodaySalesAsync(Guid.Empty);
        var monthSales = await _saleRepo.GetMonthSalesAsync(Guid.Empty);
        var avgTicket = await _saleRepo.GetAverageTicketAsync(Guid.Empty);
        var todaySalesCount = await _saleRepo.GetTodaySalesCountAsync(Guid.Empty);
        var activeCredits = await _creditRepo.GetActiveCreditsCountAsync(Guid.Empty);
        var overdueCredits = await _creditRepo.GetOverdueCreditsCountAsync(Guid.Empty);
        var monthlyRecovery = await _creditRepo.GetMonthlyRecoveryAsync(Guid.Empty);
        var totalPortfolio = await _creditRepo.GetTotalPortfolioAsync(Guid.Empty);
        var outOfStock = await _productRepo.GetOutOfStockAsync(Guid.Empty);
        var lowStock = await _productRepo.GetLowStockAsync(Guid.Empty);
        var totalProducts = await _productRepo.GetTotalCountAsync(Guid.Empty);
        var topSelling = await _productRepo.GetTopSellingAsync(Guid.Empty, 5);
        var todayIncome = await _cashRepo.GetTodayIncomeAsync(Guid.Empty);
        var todayExpense = await _cashRepo.GetTodayExpenseAsync(Guid.Empty);
        var openRegisters = await _cashRepo.GetOpenRegistersCountAsync(Guid.Empty);
        var activeEmployees = await _repo.GetActiveEmployeesAsync();
        var totalEmployees = await _repo.GetTotalEmployeesAsync();
        var pendingVac = await _repo.GetPendingVacationRequestsAsync();
        var pendingPerm = await _repo.GetPendingPermissionRequestsAsync();

        return new ExecutiveDashboardResponse(
            new CommercialKpis(todaySales, monthSales, avgTicket, todaySalesCount),
            new CreditKpis(activeCredits, overdueCredits, monthlyRecovery, totalPortfolio),
            new InventoryKpis(outOfStock.Count, lowStock.Count, totalProducts,
                topSelling.Select(p => new TopProductItem(p.Product.Name, p.TotalSold)).ToList()),
            new CashKpis(todayIncome, todayExpense, openRegisters),
            new HrKpis(activeEmployees, pendingVac, pendingPerm, totalEmployees)
        );
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
        var result = new List<RecentRequestItem>();

        var permissions = await _repo.GetRecentPermissionsAsync(count);
        var vacations = await _repo.GetRecentVacationsAsync(count);

        result.AddRange(permissions.Select(p => new RecentRequestItem(
            p.Id,
            PermissionType,
            $"{p.Employee.FirstName} {p.Employee.LastName}",
            p.Status,
            $"{p.LeaveType?.Name}",
            p.CreatedAt
        )));

        result.AddRange(vacations.Select(v => new RecentRequestItem(
            v.Id,
            VacationType,
            $"{v.Employee.FirstName} {v.Employee.LastName}",
            v.Status,
            $"{v.StartDate} - {v.EndDate}",
            v.CreatedAt
        )));

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
