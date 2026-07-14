namespace Zorvian.Application.Messages;

/// <summary>
/// Event published when a sale is cancelled.
/// Consumed by: Inventory (restore stock), Accounting (reverse entry), Credits, BI
/// </summary>
public sealed record SaleCancelledEvent
{
    public Guid SaleId { get; init; }
    public Guid CompanyId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
    public List<SaleItem> ReturnedItems { get; init; } = [];
}

// Reuses SaleItem from SaleCreatedEvent
