namespace Zorvian.Core.Entities.Fleet;

public sealed class WorkOrderPart : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? SupplierCode { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
}
