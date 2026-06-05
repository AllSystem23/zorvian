namespace Zorvian.Core.Entities;

public sealed class EmployeeBankAccount : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountType { get; set; } = "savings"; // savings, checking
    public string AccountCurrency { get; set; } = "NIO";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
