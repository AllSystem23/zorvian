namespace Zorvian.Core.Enums;

public enum CommissionType
{
    Sale,
    Collection,
    Profit,
    ProductLine,
    Category,
    Branch,
    Brand,
    Tiered,
    Goal,
    Team,
    Mixed
}

public enum CommissionCalculationMethod
{
    Percentage,
    Fixed,
    Tiered,
    Mixed
}

public enum CommissionStatus
{
    Calculated,
    Approved,
    Paid,
    ClawedBack
}
