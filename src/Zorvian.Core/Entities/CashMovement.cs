namespace Zorvian.Core.Entities;

public sealed class CashMovement : BaseEntity
{
    public Guid CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Concept { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? DocumentReference { get; set; }
    public string ApprovalStatus { get; set; } = "approved"; // draft, approved, voided
    public Guid? RelatedSaleId { get; set; }
    public Guid? RelatedCreditPaymentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid BranchId { get; set; }
}
