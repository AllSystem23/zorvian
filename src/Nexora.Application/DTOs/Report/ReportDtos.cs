namespace Nexora.Application.DTOs.Report;

public sealed record GenerateReportRequest(
    string ReportType, // vacation, permission, attendance, balance
    int? Year,
    int? Month
);
