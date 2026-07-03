namespace Zorvian.Core.Entities;

public sealed class Reconciliation : BaseEntity
{
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public string Status { get; set; } = "draft"; // draft, in_progress, completed, cancelled
    public int TotalTransactions { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal Difference { get; set; }
    public string? FileName { get; set; }
    public string? Notes { get; set; }
    public Guid? BranchId { get; set; }

    public ICollection<ReconciliationDetail> Details { get; set; } = new List<ReconciliationDetail>();
}
