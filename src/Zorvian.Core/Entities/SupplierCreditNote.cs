namespace Zorvian.Core.Entities;

public sealed class SupplierCreditNote : BaseEntity
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public DateOnly CreditNoteDate { get; set; }
    public string? Reason { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "completed";
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
