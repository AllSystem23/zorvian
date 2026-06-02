namespace Zorvian.Application.DTOs.Dashboard;

public sealed record DashboardKpisResponse(
    int TotalEmployees,
    int ActiveEmployees,
    int InactiveEmployees,
    List<DepartmentCount> EmployeesByDepartment,
    int PendingVacationRequests,
    int PendingPermissionRequests,
    int BirthdaysThisMonth,
    int WorkAnniversariesThisMonth
);

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
