namespace Zorvian.Core.Enums;

public static class SaleStatus
{
    public const string Completed = "completed";
    public const string Pending = "pending";
    public const string Cancelled = "cancelled";
    public const string Refunded = "refunded";

    public static bool IsValid(string status) => status switch
    {
        Completed or Pending or Cancelled or Refunded => true,
        _ => false,
    };
}

public static class CreditStatus
{
    public const string Active = "active";
    public const string Paid = "paid";
    public const string Defaulted = "defaulted";
    public const string Cancelled = "canceled";
    public const string Refinanced = "refinanced";

    public static bool IsValid(string status) => status switch
    {
        Active or Paid or Defaulted or Cancelled or Refinanced => true,
        _ => false,
    };
}
