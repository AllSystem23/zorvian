using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

public sealed class Quote : BaseEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly QuoteDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public QuoteStatus Status { get; set; } = QuoteStatus.Pending;
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string CurrencyCode { get; set; } = "NIO";
    public decimal? ExchangeRateToReporting { get; set; }

    public ICollection<QuoteDetail> Details { get; set; } = [];
}
