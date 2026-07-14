namespace Zorvian.Application.Messages;

/// <summary>
/// Event published when a payment is received (sale payment, credit payment).
/// Consumed by: Treasury, Accounting, Credits, BI
/// </summary>
public sealed record PaymentReceivedEvent
{
    public Guid PaymentId { get; init; }
    public Guid CompanyId { get; init; }
    public Guid? SaleId { get; init; }
    public Guid? CreditId { get; init; }
    public Guid? ClientId { get; init; }
    public decimal Amount { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string ReferenceNumber { get; init; } = string.Empty;
    public DateTime PaymentDate { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
}
