namespace Zorvian.Core.Entities.Fleet;

public sealed class RoutePoint : BaseEntity
{
    public Guid RouteId { get; set; }
    public Route Route { get; set; } = null!;
    public int Order { get; set; }
    public string Type { get; set; } = "Delivery";
    public string Address { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public TimeOnly? TimeWindowStart { get; set; }
    public TimeOnly? TimeWindowEnd { get; set; }
    public int? DurationEstMinutes { get; set; }
    public decimal? DistanceFromPreviousKm { get; set; }
    public string? Instructions { get; set; }
}
