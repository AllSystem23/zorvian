using Zorvian.Core.Entities;

namespace Zorvian.Application.DTOs.Report;

public sealed record CustomReportResponse(
    Guid Id,
    string Name,
    string? Description,
    string Module,
    List<ReportField> Fields,
    List<ReportFilter> Filters,
    string? GroupByField,
    string? SortByField,
    string SortOrder,
    bool IsPublic,
    Guid CreatedByUserId,
    DateTime CreatedAt
);

public sealed record CreateCustomReportRequest(
    string Name,
    string? Description,
    string Module,
    List<ReportField> Fields,
    List<ReportFilter> Filters,
    string? GroupByField,
    string? SortByField,
    string SortOrder,
    bool IsPublic
);

public sealed record UpdateCustomReportRequest(
    string Name,
    string? Description,
    string Module,
    List<ReportField> Fields,
    List<ReportFilter> Filters,
    string? GroupByField,
    string? SortByField,
    string SortOrder,
    bool IsPublic
);

public sealed record ReportResult(
    List<string> Columns,
    List<Dictionary<string, object?>> Rows,
    int TotalCount
);

// Existing legacy report types for ReportsController
public sealed record GenerateReportRequest(
    string ReportType,
    int? Year,
    int? Month,
    Guid? Id
);
