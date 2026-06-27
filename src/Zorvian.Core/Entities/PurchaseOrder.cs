namespace Zorvian.Core.Entities;

public sealed class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string Status { get; set; } = "draft";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }
    public Guid BranchId { get; set; }
    public string CurrencyCode { get; set; } = "NIO";
    public decimal? ExchangeRateToReporting { get; set; }
    public string CountryCode { get; set; } = "NIC";
    public Guid? PurchaseId { get; set; }

    public ICollection<PurchaseOrderDetail> Details { get; set; } = [];
}
