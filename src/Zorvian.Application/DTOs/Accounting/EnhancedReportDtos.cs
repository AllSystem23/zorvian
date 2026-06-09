namespace Zorvian.Application.DTOs.Accounting;

public sealed record EquityChangeItem(
    string Concept,
    decimal OpeningBalance,
    decimal Additions,
    decimal Deductions,
    decimal EndingBalance
);

public sealed record EquityChangeResponse(
    string PeriodName,
    DateTime GeneratedAt,
    decimal TotalOpeningEquity,
    decimal TotalAdditions,
    decimal TotalDeductions,
    decimal TotalEndingEquity,
    List<EquityChangeItem> Items
);

public sealed record CashFlowStatementItem(
    string Concept,
    decimal Amount,
    string Category
);

public sealed record CashFlowStatementResponse(
    string PeriodName,
    DateTime GeneratedAt,
    List<CashFlowStatementItem> OperatingActivities,
    decimal NetOperatingCashFlow,
    List<CashFlowStatementItem> InvestingActivities,
    decimal NetInvestingCashFlow,
    List<CashFlowStatementItem> FinancingActivities,
    decimal NetFinancingCashFlow,
    decimal NetCashIncrease,
    decimal BeginningCash,
    decimal EndingCash
);

public sealed record ComparativePeriod(
    string PeriodName,
    decimal Amount,
    decimal PercentageOfTotal
);

public sealed record ComparativeLine(
    string Concept,
    string AccountType,
    List<ComparativePeriod> Periods,
    decimal Variance,
    decimal VariancePercent
);

public sealed record ComparativeReportResponse(
    string ReportType,
    DateTime GeneratedAt,
    List<ComparativeLine> Lines,
    decimal TotalCurrent,
    decimal TotalPrevious,
    decimal TotalVariance,
    decimal TotalVariancePercent
);

public sealed record FinancialAssistantRequest(
    string Query
);

public sealed record FinancialAssistantResponse(
    string Answer,
    string? Confidence,
    List<FinancialAssistantDataPoint>? SupportingData
);

public sealed record FinancialAssistantDataPoint(
    string Label,
    decimal Value,
    string? Format
);

public sealed record ComparativeReportRequest(
    string ReportType,
    List<Guid> PeriodIds
);
