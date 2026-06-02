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
    public string Status { get; set; } = "pending";
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }

    public ICollection<QuoteDetail> Details { get; set; } = [];
}
