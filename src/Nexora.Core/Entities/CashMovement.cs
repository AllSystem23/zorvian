namespace Nexora.Core.Entities;

public sealed class CashMovement : BaseEntity
{
    public Guid CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public string MovementType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Concept { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid? RelatedSaleId { get; set; }
    public Guid? RelatedCreditPaymentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
