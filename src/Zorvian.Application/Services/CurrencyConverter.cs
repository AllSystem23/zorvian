namespace Zorvian.Application.Services;

public static class CurrencyConverter
{
    public static decimal ToReporting(decimal amount, string? currencyCode, decimal? exchangeRateToReporting, string companyCurrency = "NIO")
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return amount;
        if (string.Equals(currencyCode, companyCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;
        if (!exchangeRateToReporting.HasValue || exchangeRateToReporting.Value <= 0)
            return amount;
        return amount * exchangeRateToReporting.Value;
    }
}
