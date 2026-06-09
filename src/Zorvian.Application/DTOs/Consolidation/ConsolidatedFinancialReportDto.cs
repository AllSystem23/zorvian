namespace Zorvian.Application.DTOs.Consolidation;

public record ConsolidatedFinancialReportDto(
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal NetIncome,
    IEnumerable<ConsolidatedLineItemDto> Lines
);

public record ConsolidatedLineItemDto(
    string Concept,
    decimal TotalAmount
);

public record IntercompanyTransactionDto(
    Guid Id,
    Guid FromCompanyId,
    Guid ToCompanyId,
    decimal Amount,
    string Currency,
    string Description,
    string Status
);
