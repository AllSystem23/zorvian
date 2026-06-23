namespace Zorvian.Core.Entities;

public sealed class Account : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string NormalSide { get; set; } = "Debit";
    public Guid? ParentId { get; set; }
    public Account? Parent { get; set; }
    public int Level { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public decimal OpeningBalance { get; set; }
    public Guid? CostCenterId { get; set; }
    public CostCenter? CostCenter { get; set; }
    public ICollection<Account> Children { get; set; } = [];
}

public static class AccountTypes
{
    public const string Asset = "Asset";
    public const string Liability = "Liability";
    public const string Equity = "Equity";
    public const string Income = "Income";
    public const string Cost = "Cost";
    public const string Expense = "Expense";
}

public static class AccountSide
{
    public const string Debit = "Debit";
    public const string Credit = "Credit";
}
