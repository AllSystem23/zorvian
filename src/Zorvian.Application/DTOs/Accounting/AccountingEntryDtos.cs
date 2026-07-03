using Zorvian.Core.Entities;

namespace Zorvian.Application.DTOs.Accounting;

public sealed record AccountingEntryDetailItem(
    Guid AccountId,
    string? AccountCode,
    string? AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description
);

public sealed record AccountingEntryResponse(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    string Description,
    string ReferenceType,
    Guid? ReferenceId,
    string Status,
    Guid AccountingPeriodId,
    string? PeriodName,
    decimal TotalDebit,
    decimal TotalCredit,
    DateTime CreatedAt,
    DateTime? PostedAt,
    string? CreatedBy,
    List<AccountingEntryDetailItem> Details
);

public sealed record AccountingEntryListResponse(
    Guid Id,
    string EntryNumber,
    DateTime EntryDate,
    string Description,
    string ReferenceType,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string? PeriodName
);

public sealed record CreateManualEntryRequest(
    DateTime EntryDate,
    string Description,
    Guid AccountingPeriodId,
    List<CreateEntryDetailRequest> Details
);

public sealed record CreateEntryDetailRequest(
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description
);

public sealed record AccountingPeriodResponse(
    Guid Id,
    int Year,
    int Month,
    string Name,
    string Status,
    DateTime? OpenedAt,
    DateTime? ClosedAt,
    Guid? FiscalYearId = null,
    string? FiscalYearName = null,
    string? CloseNotes = null,
    DateTime? ReopenedAt = null,
    string? ReopenReason = null
);

public sealed record OpenPeriodRequest(
    int Year,
    int Month,
    Guid? FiscalYearId = null
);

public sealed record ClosePeriodRequest(
    string? Notes = null
);

public sealed record ReopenPeriodRequest(
    string Reason
);

public sealed record OpenFiscalYearRequest(
    int Year,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null
);

public sealed record TrialBalanceItem(
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal OpeningBalance,
    decimal DebitMovements,
    decimal CreditMovements,
    decimal EndingBalance
);

public sealed record TrialBalanceResponse(
    Guid PeriodId,
    string PeriodName,
    DateTime GeneratedAt,
    decimal TotalOpeningDebit,
    decimal TotalOpeningCredit,
    decimal TotalMovementsDebit,
    decimal TotalMovementsCredit,
    decimal TotalEndingDebit,
    decimal TotalEndingCredit,
    List<TrialBalanceItem> Items
);

public sealed record IncomeStatementItem(
    string AccountCode,
    string AccountName,
    decimal Balance
);

public sealed record IncomeStatementResponse(
    string PeriodName,
    decimal TotalIncome,
    decimal TotalCost,
    decimal GrossProfit,
    decimal TotalExpenses,
    decimal NetIncome
);

public sealed record BalanceSheetResponse(
    string PeriodName,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    List<BalanceSheetSection> Sections
);

public sealed record BalanceSheetSection(
    string Title,
    string Type,
    List<BalanceSheetItem> Items
);

public sealed record BalanceSheetItem(
    string AccountCode,
    string AccountName,
    decimal Balance
);

public sealed record GeneralLedgerRequest(
    Guid? AccountId,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page = 1,
    int? PageSize = 50
);

public sealed record GeneralLedgerItem(
    DateTime Date,
    string EntryNumber,
    string Description,
    string? ReferenceType,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal RunningBalance
);

public sealed record GeneralLedgerResponse(
    string AccountCode,
    string AccountName,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal EndingBalance,
    List<GeneralLedgerItem> Items
);

public sealed record CostCenterExpenseItem(
    string AccountCode,
    string AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    decimal Balance
);

public sealed record CostCenterExpenseReport(
    Guid CostCenterId,
    string CostCenterName,
    string CostCenterCode,
    DateTime GeneratedAt,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal TotalBalance,
    List<CostCenterExpenseItem> Items
);

public sealed record BudgetVsActualItem(
    Guid BudgetId,
    int Year,
    int Month,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    Guid? CostCenterId,
    string? CostCenterName,
    decimal BudgetedAmount,
    decimal ActualAmount,
    decimal Variance,
    decimal VariancePercent
);

public sealed record BudgetVsActualReport(
    int Year,
    int Month,
    DateTime GeneratedAt,
    decimal TotalBudgeted,
    decimal TotalActual,
    decimal TotalVariance,
    List<BudgetVsActualItem> Items
);
