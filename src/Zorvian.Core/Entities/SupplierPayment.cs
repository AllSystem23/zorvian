namespace Zorvian.Core.Entities;

public sealed class SupplierPayment : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
