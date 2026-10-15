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

    /// <summary>
    /// Convierte el valor de la API (string en minúsculas) al enum.
    /// Acepta sinónimos comerciales ("approved"/"won"/"lost") para no romper el frontend.
    /// Lanza <see cref="ArgumentException"/> si el valor no es reconocido.
    /// </summary>
    public static QuoteStatus Parse(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "pending" => QuoteStatus.Pending,
        "sent" => QuoteStatus.Sent,
        "accepted" or "approved" => QuoteStatus.Accepted,
        "rejected" or "lost" => QuoteStatus.Rejected,
        "expired" => QuoteStatus.Expired,
        "converted" or "won" => QuoteStatus.Converted,
        _ => throw new ArgumentException($"Estado de cotización inválido: '{value}'", nameof(value))
    };
}
