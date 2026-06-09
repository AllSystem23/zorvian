namespace Zorvian.Core.Enums;

public enum QuoteStatus
{
    Pending,
    Sent,
    Accepted,
    Rejected,
    Expired,
    Converted
}

public static class QuoteStatusExtensions
{
    public static string ToDbValue(this QuoteStatus status) => status switch
    {
        QuoteStatus.Pending => "pending",
        QuoteStatus.Sent => "sent",
        QuoteStatus.Accepted => "accepted",
        QuoteStatus.Rejected => "rejected",
        QuoteStatus.Expired => "expired",
        QuoteStatus.Converted => "converted",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
