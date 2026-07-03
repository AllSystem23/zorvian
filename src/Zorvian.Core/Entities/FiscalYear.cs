namespace Zorvian.Core.Entities;

public sealed class FiscalYear : BaseEntity
{
    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = FiscalYearStatus.Open;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? AuditedAt { get; set; }
    public string? AuditedBy { get; set; }

    public ICollection<AccountingPeriod> Periods { get; set; } = [];
}

public static class FiscalYearStatus
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Audited = "audited";
}
