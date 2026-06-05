namespace Zorvian.Core.Entities;

public sealed class AccountLink : BaseEntity
{
    public string TransactionType { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public Guid CompanyId { get; set; }
}

public static class TransactionTypes
{
    public const string Sale = "Sale";
    public const string Purchase = "Purchase";
    public const string InventoryMovement = "InventoryMovement";
    public const string CreditPayment = "CreditPayment";
    public const string CashMovement = "CashMovement";
    public const string Payroll = "Payroll";
}

public static class AccountRoles
{
    public const string Inventory = "Inventory";
    public const string AccountsReceivable = "AccountsReceivable";
    public const string AccountsPayable = "AccountsPayable";
    public const string SalesRevenue = "SalesRevenue";
    public const string CostOfSales = "CostOfSales";
    public const string VatPayable = "VatPayable";
    public const string VatReceivable = "VatReceivable";
    public const string Cash = "Cash";
    public const string Bank = "Bank";
    public const string RetainedEarnings = "RetainedEarnings";
    public const string InventoryAdjustment = "InventoryAdjustment";
    public const string PurchaseExpense = "PurchaseExpense";
    public const string ContraAccount = "ContraAccount";
}
