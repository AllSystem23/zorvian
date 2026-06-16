namespace Zorvian.Core.Entities.Fleet;

public sealed class DriverInfraction : BaseEntity
{
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public DateTime InfractionDate { get; set; }
    public string? Description { get; set; }
    public decimal? FineAmount { get; set; }
    public int? Points { get; set; }
    public string Status { get; set; } = "Open";
}
