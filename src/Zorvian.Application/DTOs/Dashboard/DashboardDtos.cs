namespace Zorvian.Application.DTOs.Dashboard;

public sealed record DashboardKpisResponse(
    int TotalEmployees,
    int ActiveEmployees,
    int InactiveEmployees,
    double? ActiveEmployeesTrend,
    double? PendingRequestsTrend,
    double? BirthdaysTrend,
    double? AnniversariesTrend,
    List<DepartmentCount> EmployeesByDepartment,
    int PendingVacationRequests,
    int PendingPermissionRequests,
    int BirthdaysThisMonth,
    int WorkAnniversariesThisMonth
)
{
    public static DashboardKpisResponse Empty => new(
        0,
        0,
        0,
        null,
        null,
        null,
        null,
        [],
        0,
        0,
        0,
        0
    );
}

public sealed record DepartmentCount(
    string DepartmentName,
    int Count
);

public sealed record VacationCalendarEvent(
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string Type,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status
);

public sealed record RecentRequestItem(
    Guid Id,
    string RequestType,
    string EmployeeName,
    string Status,
    string? Description,
    DateTime CreatedAt
);

public sealed record DashboardSummaryResponse(
    DashboardKpisResponse Kpis,
    List<VacationCalendarEvent> CalendarEvents,
    List<RecentRequestItem> RecentRequests
)
{
    public static DashboardSummaryResponse Empty => new(
        DashboardKpisResponse.Empty,
        [],
        []
    );
}

/// <summary>
/// Raw SQL result DTO — all scalar KPI values in a single DB round-trip.
/// Property names match the SQL column aliases (case-insensitive).
/// </summary>
public sealed class DashboardKpiScalars
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int PendingVacationRequests { get; set; }
    public int PendingPermissionRequests { get; set; }
    public int BirthdayCount { get; set; }
    public int AnniversaryCount { get; set; }
    public int PrevBirthdayCount { get; set; }
    public int PrevAnniversaryCount { get; set; }
    public int ResolvedVacationCount { get; set; }
    public int ResolvedPermissionCount { get; set; }
    public int PreviousMonthActiveEmployees { get; set; }
}

public sealed class ExecutiveKpiScalars
{
    public decimal TodaySales { get; set; }
    public decimal MonthSales { get; set; }
    public decimal AverageTicket { get; set; }
    public int TodaySalesCount { get; set; }
    public int ActiveCredits { get; set; }
    public int OverdueCredits { get; set; }
    public decimal MonthlyRecovery { get; set; }
    public decimal TotalPortfolio { get; set; }
    public int OutOfStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int TotalProducts { get; set; }
    public decimal TodayIncome { get; set; }
    public decimal TodayExpense { get; set; }
    public int OpenRegisters { get; set; }
    public int ActiveEmployees { get; set; }
    public int TotalEmployees { get; set; }
    public int PendingVacations { get; set; }
    public int PendingPermissions { get; set; }
}

public sealed record PayrollDashboardResponse(
    List<PayrollCostByDept> CostsByDepartment,
    List<PayrollHistoryItem> History,
    decimal TotalLastPayroll
);

public sealed record PayrollCostByDept(
    string Department,
    decimal Amount
);

public sealed record PayrollHistoryItem(
    string Period,
    decimal Amount
);

