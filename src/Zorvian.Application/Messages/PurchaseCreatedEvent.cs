namespace Zorvian.Application.Messages;

/// <summary>
/// Event published when a purchase is created.
/// Consumed by: AutoAccounting, Inventory, Budget
/// </summary>
public sealed record PurchaseCreatedEvent
{
    public Guid PurchaseId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid SupplierId { get; init; }
    public decimal Total { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public List<PurchaseItem> Items { get; init; } = [];
}

public sealed record PurchaseItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitCost { get; init; }
    public decimal Subtotal { get; init; }
}
