namespace Zorvian.Core.Entities.Fleet;

public sealed class FuelRefill : BaseEntity
{
    public DateTime RefillDateTime { get; set; }
    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = null!;
    public Guid FuelTypeId { get; set; }
    public FuelType FuelType { get; set; } = null!;
    public decimal Liters { get; set; }
    public decimal PricePerLiter { get; set; }
    public decimal TotalCost { get; set; }
    public decimal CurrentKm { get; set; }
    public decimal? HourMeter { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string RefillType { get; set; } = "Full";
    public string PaymentMethod { get; set; } = "Cash";
    public string? InvoiceUrl { get; set; }
    public string? Observations { get; set; }
    public bool ValidForCalculation { get; set; } = true;
    public bool AnomalyFlag { get; set; }
    public string? AnomalyNotes { get; set; }
}
