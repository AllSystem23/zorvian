namespace Zorvian.Core.Entities;

public sealed class WarrantyPartUsage : BaseEntity
{
    public Guid ClaimId { get; set; }
    public WarrantyClaim Claim { get; set; } = null!;
    public Guid? PartReceiptId { get; set; }
    public WarrantyPartReceipt? PartReceipt { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityUsed { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public Guid? UsedByEmployeeId { get; set; }
    public Employee? UsedBy { get; set; }
    public string? Notes { get; set; }
}
