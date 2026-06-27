namespace Zorvian.Core.Entities;

public sealed class Purchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime? PurchaseDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? InvoiceReference { get; set; }
    public string Status { get; set; } = "completed";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
    public string? WithholdingType { get; set; }
    public decimal? WithholdingRate { get; set; }
    public decimal? WithholdingAmount { get; set; }
    public Guid BranchId { get; set; }
    public string CurrencyCode { get; set; } = "NIO";
    public decimal? ExchangeRateToReporting { get; set; }
    public string CountryCode { get; set; } = "NIC";
    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public ICollection<PurchaseDetail> Details { get; set; } = [];
    public ICollection<SupplierPayment> Payments { get; set; } = [];
    public ICollection<SupplierCreditNote> CreditNotes { get; set; } = [];
}
