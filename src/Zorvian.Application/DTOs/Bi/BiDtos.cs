namespace Zorvian.Application.DTOs.Bi;

public sealed record BiExecutiveResponse(
    BiSalesKpi Sales,
    BiCreditKpi Credits,
    BiInventoryKpi Inventory,
    BiCashKpi Cash,
    BiHrKpi HumanResources,
    List<BiAlertItem> Alerts
);

public sealed record BiSalesKpi(
    decimal TodaySales,
    decimal YesterdaySales,
    double SalesChangePercent,
    decimal MonthSales,
    double MonthSalesChangePercent,
    decimal AverageTicket,
    int TodaySalesCount,
    List<decimal> WeeklyTrend
);

public sealed record BiCreditKpi(
    int ActiveCredits,
    int OverdueCredits,
    decimal MonthlyRecovery,
    decimal TotalPortfolio,
    double CollectionRate,
    double DsoDays
);

public sealed record BiInventoryKpi(
    int OutOfStockProducts,
    int LowStockProducts,
    int TotalProducts,
    decimal TotalStockValue,
    double TurnoverRate,
    List<BiTopProductItem> TopSelling
);

public sealed record BiTopProductItem(string ProductName, int TotalSold);

public sealed record BiCashKpi(
    decimal TodayIncome,
    decimal TodayExpense,
    decimal NetCashFlow,
    int OpenRegisters
);

public sealed record BiHrKpi(
    int ActiveEmployees,
    int TotalEmployees,
    decimal PayrollTotal,
    decimal AvgCostPerEmployee,
    int PendingRequests
);

public sealed record BiAlertItem(string Type, string Message, string Severity);

public sealed record BiSalesTrendResponse(
    List<BiMonthSales> Monthly,
    double ChangePercent,
    decimal Total,
    decimal AveragePerMonth
);

public sealed record BiMonthSales(
    int Year, int Month,
    decimal Total, int Count,
    decimal AverageTicket, decimal TotalCost, decimal Margin
);

public sealed record BiSalesByCategoryResponse(
    List<BiCategorySalesItem> Items
);

public sealed record BiCategorySalesItem(
    string CategoryName, decimal Total, int Quantity, double Percentage
);

public sealed record BiSalesBySellerItem(
    string EmployeeName, decimal Total, int Count, decimal AverageTicket
);

public sealed record BiQuoteConversionResponse(
    int TotalQuotes, int Converted, double ConversionRate,
    double AverageDaysToConvert
);

public sealed record BiArAgingResponse(
    decimal Current, decimal Days30, decimal Days60,
    decimal Days90, decimal Days90Plus,
    decimal TotalOverdue, decimal TotalPortfolio,
    double OverduePercent,
    List<BiClientAgingItem> ByClient
);

public sealed record BiClientAgingItem(
    string ClientName, decimal Balance,
    decimal Current, decimal Days30, decimal Days60,
    decimal Days90, decimal Days90Plus
);

public sealed record BiApAgingResponse(
    decimal Current, decimal Days30, decimal Days60,
    decimal Days90, decimal Days90Plus,
    decimal TotalOverdue, decimal TotalPayable,
    double OverduePercent,
    List<BiSupplierAgingItem> BySupplier
);

public sealed record BiSupplierAgingItem(
    string SupplierName, decimal Balance,
    decimal Current, decimal Days30, decimal Days60,
    decimal Days90, decimal Days90Plus
);

public sealed record BiFinancialRatiosResponse(
    double CurrentRatio, double QuickRatio,
    double DebtRatio, double DebtToEquity,
    double GrossMargin, double NetMargin,
    double OperatingMargin, double Roa, double Roe,
    decimal WorkingCapital, decimal BreakEvenPoint
);

public sealed record BiComparativeIncomeResponse(
    BiIncomeStatement Current,
    BiIncomeStatement Previous,
    List<BiLineItemChange> Changes
);

public sealed record BiIncomeStatement(
    string PeriodName, decimal TotalIncome,
    decimal TotalCost, decimal GrossProfit,
    decimal TotalExpenses, decimal NetIncome
);

public sealed record BiLineItemChange(
    string Name, decimal CurrentValue,
    decimal PreviousValue, double ChangePercent
);

public sealed record BiCashFlowResponse(
    List<BiCashFlowItem> Operating,
    List<BiCashFlowItem> Investing,
    List<BiCashFlowItem> Financing,
    decimal NetIncrease, decimal BeginningCash, decimal EndingCash,
    DateTime GeneratedAt
);

public sealed record BiCashFlowItem(
    string Description, decimal Amount, string Type
);

public sealed record BiInventorySummaryResponse(
    decimal TotalValue, int TotalProducts,
    int LowStockCount, int OutOfStockCount,
    double TurnoverRate, decimal DeadStockValue,
    List<BiCategoryInventoryItem> ByCategory,
    List<BiSlowMoverItem> TopSlowMovers
);

public sealed record BiCategoryInventoryItem(
    string CategoryName, int Count,
    decimal TotalCost, decimal TotalValue
);

public sealed record BiSlowMoverItem(
    string ProductName, int Stock, int MonthsWithoutMovement
);

public sealed record BiPayrollSummaryResponse(
    List<BiPayrollCostByDept> ByDepartment,
    List<BiPayrollTrendItem> Trend,
    decimal TotalCost, decimal AveragePerEmployee,
    decimal OvertimeTotal, decimal EmployerCostTotal
);

public sealed record BiPayrollCostByDept(
    string Department, decimal Amount, int EmployeeCount
);

public sealed record BiPayrollTrendItem(
    string Period, decimal GrossPay, decimal Deductions,
    decimal NetPay, decimal EmployerCosts
);

public sealed record BiEmployeeTurnoverResponse(
    int Hired, int Terminated,
    double TurnoverRate, int TotalActive,
    List<BiTurnoverByDept> ByDepartment
);

public sealed record BiTurnoverByDept(
    string Department, int Hired, int Terminated, double Rate
);
