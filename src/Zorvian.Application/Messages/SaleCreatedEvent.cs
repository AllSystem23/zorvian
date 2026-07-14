namespace Zorvian.Application.Messages;

/// <summary>
/// Event published when a sale is created.
/// Consumed by: CommissionEngine, GoalEngine, Inventory, Accounting, BI
/// </summary>
public sealed record SaleCreatedEvent
{
    public Guid SaleId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid ClientId { get; init; }
    public Guid? EmployeeId { get; init; }
    public decimal Total { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Tax { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime SaleDate { get; init; }
    public string SaleType { get; init; } = string.Empty;
    public List<SaleItem> Items { get; init; } = [];
}

public sealed record SaleItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
