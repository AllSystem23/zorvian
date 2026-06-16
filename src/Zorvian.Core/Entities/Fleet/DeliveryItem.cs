namespace Zorvian.Core.Entities.Fleet;

public sealed class DeliveryItem : BaseEntity
{
    public Guid DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal QtyOrdered { get; set; }
    public decimal QtyDelivered { get; set; }
    public decimal QtyReturned { get; set; }
    public string? LotSerial { get; set; }
    public string Status { get; set; } = "Pending";
}
