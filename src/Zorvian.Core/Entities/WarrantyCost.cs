namespace Zorvian.Core.Entities;

public sealed class WarrantyCost : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid? ClaimId { get; set; }
    public WarrantyClaim? Claim { get; set; }
    public string CostCategory { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitCost { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public string PaidBy { get; set; } = string.Empty;
    public Guid? PaidByPartyId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? InvoiceUrl { get; set; }
    public bool IsBilled { get; set; }
    public Guid? AccountingEntryId { get; set; }
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public Guid? RegisteredByEmployeeId { get; set; }
    public Employee? RegisteredBy { get; set; }
}
