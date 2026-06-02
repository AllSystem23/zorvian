namespace Nexora.Core.Entities;

public sealed class SaleDetail : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
