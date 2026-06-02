namespace Nexora.Core.Entities;

public sealed class SalePayment : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; }
    public Guid? CashRegisterId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
}
