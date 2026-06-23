namespace Zorvian.Core.Entities;

public sealed class Withholding : BaseEntity
{
    public Guid PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;
    public string WithholdingType { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal BaseAmount { get; set; }
    public decimal Amount { get; set; }
    public string? CertificateNumber { get; set; }
    public DateOnly? IssueDate { get; set; }
    public string Status { get; set; } = "pending";
    public Guid BranchId { get; set; }
}
