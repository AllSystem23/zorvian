namespace Zorvian.Core.Entities;

public sealed class WarrantyPartRequest : BaseEntity
{
    public Guid WarrantyId { get; set; }
    public Warranty Warranty { get; set; } = null!;
    public Guid ClaimId { get; set; }
    public WarrantyClaim Claim { get; set; } = null!;
    public Guid ProviderId { get; set; }
    public WarrantyProvider Provider { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int QuantityRequested { get; set; }
    public int QuantityReceived { get; set; }
    public decimal? UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string Status { get; set; } = "requested";
    public string? ProviderAuthorizationCode { get; set; }
    public string? ProviderNotes { get; set; }
    public string? InternalNotes { get; set; }
    public Guid? RequestedByEmployeeId { get; set; }
    public Employee? RequestedBy { get; set; }
    public Guid? ApprovedByEmployeeId { get; set; }
    public Employee? ApprovedBy { get; set; }
}
