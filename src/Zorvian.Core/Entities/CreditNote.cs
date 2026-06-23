namespace Zorvian.Core.Entities;

public sealed class CreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public string Status { get; set; } = "issued";
    public string Reason { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<CreditNoteDetail> Details { get; set; } = [];
}
