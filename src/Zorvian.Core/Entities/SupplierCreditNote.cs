namespace Zorvian.Core.Entities;

public sealed class SupplierCreditNote : BaseEntity
{
    public Guid BranchId { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;

    // Purchase module fields
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }
    public DateOnly CreditNoteDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Warranty module fields
    public Guid? WarrantyId { get; set; }
    public Warranty? Warranty { get; set; }
    public Guid? WarrantyPartRequestId { get; set; }
    public WarrantyPartRequest? WarrantyPartRequest { get; set; }
    public Guid? WarrantyProviderId { get; set; }
    public WarrantyProvider? WarrantyProvider { get; set; }
    public Guid? WarrantyCostId { get; set; }
    public WarrantyCost? WarrantyCost { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "issued";
    public string? Notes { get; set; }
}
