namespace Zorvian.Core.Enums;

public enum ServiceContractStatus
{
    Draft,
    Active,
    Completed,
    Terminated,
    Cancelled
}

public enum PaymentMilestoneStatus
{
    Pending,
    InProgress,
    Completed,
    Approved,
    Paid,
    Cancelled
}

public enum ProviderInvoiceStatus
{
    Received,
    Verified,
    Approved,
    Paid,
    Cancelled
}

public enum RankingType
{
    Salesperson,
    Collector,
    Technician,
    Branch
}
