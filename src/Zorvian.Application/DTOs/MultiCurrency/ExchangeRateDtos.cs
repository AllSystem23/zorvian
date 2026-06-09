namespace Zorvian.Application.DTOs.MultiCurrency;

public sealed record ExchangeRateResponse(
    Guid Id,
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime EffectiveDate
);

public sealed record CreateExchangeRateRequest(
    string FromCurrency,
    string ToCurrency,
    decimal Rate,
    DateTime EffectiveDate
);

public sealed record UpdateExchangeRateRequest(
    decimal Rate,
    DateTime EffectiveDate
);
