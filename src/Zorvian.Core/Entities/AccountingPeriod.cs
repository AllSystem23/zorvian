namespace Zorvian.Core.Entities;

public sealed class AccountingPeriod : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "open";
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? FiscalYearId { get; set; }
    public FiscalYear? FiscalYear { get; set; }
    public string? CloseNotes { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public string? ReopenReason { get; set; }

    public ICollection<AccountingEntry> Entries { get; set; } = [];
}

public static class PeriodStatus
{
    public const string Open = "open";
    public const string Closed = "closed";
    public const string Locked = "locked";
}
