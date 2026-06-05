namespace Zorvian.Core.Entities;

public sealed class PurchaseDetail : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
