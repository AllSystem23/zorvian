namespace Zorvian.Application.DTOs.Provider;

public sealed record ProviderRankingDto(
    Guid ProviderId,
    string BusinessName,
    string EmployeeName,
    int Rank,
    decimal OverallScore,
    decimal OnTimeDeliveryScore,
    decimal ContractCompletionScore,
    decimal InvoiceAccuracyScore,
    int TotalContracts,
    int CompletedContracts,
    int TotalMilestones,
    int OnTimeMilestones,
    int TotalInvoices,
    int AccurateInvoices,
    string Trend // up, down, stable
);

public sealed record ProviderRankingDetailDto(
    List<ProviderRankingDto> Rankings,
    decimal AverageOverallScore,
    int TotalProviders,
    DateTime GeneratedAt
);

public sealed record ProviderRankingHistoryDto(
    List<ProviderRankingHistorySeries> Series,
    List<ProviderRankingHistoryPoint> AllPoints,
    DateTime GeneratedAt
);

public sealed record ProviderRankingHistorySeries(
    Guid ProviderId,
    string BusinessName,
    List<ProviderRankingHistoryPoint> Points
);

public sealed record ProviderRankingHistoryPoint(
    string Month,
    decimal OverallScore,
    decimal OnTimeDeliveryScore,
    decimal ContractCompletionScore,
    decimal InvoiceAccuracyScore
);
