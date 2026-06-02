namespace Nexora.Core.Entities;

public sealed class QuoteDetail : BaseEntity
{
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
