namespace Zorvian.Application.DTOs.Report;

public sealed record GenerateReportRequest(
    string ReportType, // vacation, permission, attendance, balance, pay-stub, payroll-cost
    int? Year,
    int? Month,
    Guid? Id
);
