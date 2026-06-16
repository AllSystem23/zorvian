namespace Zorvian.Core.Entities.Fleet;

public sealed class FleetExpense : BaseEntity
{
    public DateTime ExpenseDate { get; set; }
    public Guid CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public Guid? SubcategoryId { get; set; }
    public ExpenseSubcategory? Subcategory { get; set; }
    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public Guid? DriverId { get; set; }
    public Driver? Driver { get; set; }
    public Guid? TripId { get; set; }
    public Trip? Trip { get; set; }
    public Guid? RouteId { get; set; }
    public Route? Route { get; set; }
    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "NIO";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal AmountBaseCurrency { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? DocumentUrl { get; set; }
    public bool Reimbursable { get; set; }
    public bool Reimbursed { get; set; }
    public bool Approved { get; set; }
    public Guid? AccountId { get; set; }
    public Account? Account { get; set; }
}
