namespace Zorvian.Core.Entities;

public sealed class WarrantyPartReceipt : BaseEntity
{
    public Guid PartRequestId { get; set; }
    public WarrantyPartRequest PartRequest { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public int QuantityReceived { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string? BatchLot { get; set; }
    public string? SerialNumber { get; set; }
    public string? Condition { get; set; }
    public Guid? StorageLocationId { get; set; }
    public Location? StorageLocation { get; set; }
    public Guid? InventoryMovementId { get; set; }
    public Guid? ReceivedByEmployeeId { get; set; }
    public Employee? ReceivedBy { get; set; }
    public string? Notes { get; set; }
}
