namespace Zorvian.Core.Entities;

public enum FrequencyType
{
    Weekly,
    BiWeekly,
    SemiMonthly,
    Monthly
}

public sealed class PayrollPeriod : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public int PeriodNumber { get; set; }
    public FrequencyType Frequency { get; set; } = FrequencyType.Monthly;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly PaymentDate { get; set; }
    public string Status { get; set; } = "open";
}
