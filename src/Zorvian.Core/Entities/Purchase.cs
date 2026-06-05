namespace Zorvian.Core.Entities;

public sealed class Purchase : BaseEntity
{
    public string PurchaseNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime? PurchaseDate { get; set; }
    public string? InvoiceReference { get; set; }
    public string Status { get; set; } = "completed";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<PurchaseDetail> Details { get; set; } = [];
}
