namespace Nexora.Core.Entities;

public sealed class InventoryMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public decimal UnitCost { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? PerformedByEmployeeId { get; set; }
    public Employee? PerformedBy { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
