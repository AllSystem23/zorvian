namespace Zorvian.Core.Entities;

public sealed class ReconciliationDetail : BaseEntity
{
    public Guid ReconciliationId { get; set; }
    public Reconciliation Reconciliation { get; set; } = null!;
    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionType { get; set; } = string.Empty; // debit, credit
    public DateOnly TransactionDate { get; set; }
    public string? Description { get; set; }
    public string SourceType { get; set; } = string.Empty; // bank_statement, system
    public string? SourceId { get; set; } // FK to related entity (PayrollDetailId, SaleId, etc.)
    public string MatchStatus { get; set; } = "unmatched"; // matched, unmatched, pending
    public Guid? MatchedDetailId { get; set; } // FK to matching system detail
    public string? Notes { get; set; }
}
