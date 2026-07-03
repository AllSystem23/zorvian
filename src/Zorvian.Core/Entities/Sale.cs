namespace Zorvian.Core.Entities;

public sealed class Sale : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateTime SaleDate { get; set; }
    public string SaleType { get; set; } = "cash";
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = Enums.SaleStatus.Completed;
    public string? Notes { get; set; }
    public Guid BranchId { get; set; }
    public string CurrencyCode { get; set; } = "NIO";
    public decimal? ExchangeRateToReporting { get; set; }

    public ICollection<SaleDetail> Details { get; set; } = [];
    public ICollection<SalePayment> Payments { get; set; } = [];
    public Credit? Credit { get; set; }
}
