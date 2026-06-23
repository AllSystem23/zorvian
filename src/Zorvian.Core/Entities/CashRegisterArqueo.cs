namespace Zorvian.Core.Entities;

public sealed class CashRegisterArqueo : BaseEntity
{
    public Guid CashRegisterId { get; set; }
    public CashRegister CashRegister { get; set; } = null!;
    public decimal ExpectedBalance { get; set; }
    public decimal CountedTotal { get; set; }
    public decimal Difference { get; set; }
    public string? Notes { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public Guid BranchId { get; set; }
    public ICollection<CashArqueoDenomination> Denominations { get; set; } = [];
}
