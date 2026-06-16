namespace Zorvian.Core.Entities.Fleet;

public sealed class Delivery : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    public string DeliveryAddress { get; set; } = string.Empty;
    public DateOnly ScheduledDate { get; set; }
    public TimeOnly? TimeWindowStart { get; set; }
    public TimeOnly? TimeWindowEnd { get; set; }
    public Guid? RouteId { get; set; }
    public Route? Route { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? DeliveredAt { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverId { get; set; }
    public string? SignatureUrl { get; set; }
    public string? PhotosJson { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
    public string? Observations { get; set; }
    public string? DocumentUrl { get; set; }
    public ICollection<DeliveryItem> Items { get; set; } = [];
}
