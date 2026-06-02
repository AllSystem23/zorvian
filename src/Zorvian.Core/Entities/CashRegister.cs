namespace Zorvian.Core.Entities;

public sealed class CashRegister : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal ExpectedBalance { get; set; }
    public decimal Difference { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string Status { get; set; } = "open";
    public string? Notes { get; set; }
    public Guid CompanyId { get; set; }

    public ICollection<CashMovement> Movements { get; set; } = [];
}
