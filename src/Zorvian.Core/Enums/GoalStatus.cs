namespace Zorvian.Core.Enums;

public enum GoalType
{
    Sales,
    Collection,
    Profit,
    Billing,
    NewClients,
    Warranties,
    Services,
    Deliveries,
    Production,
    Support
}

public enum GoalEvaluationFrequency
{
    Monthly,
    Quarterly,
    Yearly,
    Custom
}

public enum IncentiveType
{
    Bonus,
    CommissionMultiplier,
    FixedAmount,
    PercentageOfSalary
}

public enum IncentivePaymentTrigger
{
    Automatic,
    ManualApproval
}

public enum GoalStatus
{
    Active,
    Completed,
    Expired,
    Cancelled
}
