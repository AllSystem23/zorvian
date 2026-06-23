namespace Zorvian.Core.Entities;

public sealed class CreditNoteDetail : BaseEntity
{
    public Guid CreditNoteId { get; set; }
    public CreditNote CreditNote { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public Guid BranchId { get; set; }
}
