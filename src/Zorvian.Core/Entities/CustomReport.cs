namespace Zorvian.Core.Entities;

public sealed record ReportField(
    string Name,
    string Source,
    string DataType,
    bool Visible,
    int Order,
    string? Aggregate
);

public sealed record ReportFilter(
    string Field,
    string Operator,
    string Value,
    string? Value2
);

public sealed class CustomReport : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Module { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
    public string FiltersJson { get; set; } = "[]";
    public string? GroupByField { get; set; }
    public string? SortByField { get; set; }
    public string SortOrder { get; set; } = "asc";
    public bool IsPublic { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid CompanyId { get; set; }
}
