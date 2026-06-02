namespace Zorvian.Application.DTOs.Dashboard;

public sealed record ExecutiveDashboardResponse(
    CommercialKpis Commercial,
    CreditKpis Credits,
    InventoryKpis Inventory,
    CashKpis Cash,
    HrKpis HumanResources
);

public sealed record CommercialKpis(
    decimal TodaySales,
    decimal MonthSales,
    decimal AverageTicket,
    int TodaySalesCount
);

public sealed record CreditKpis(
    int ActiveCredits,
    int OverdueCredits,
    decimal MonthlyRecovery,
    decimal TotalPortfolio
);

public sealed record InventoryKpis(
    int OutOfStockProducts,
    int LowStockProducts,
    int TotalProducts,
    List<TopProductItem> TopSelling
);

public sealed record TopProductItem(
    string ProductName,
    int TotalSold
);

public sealed record CashKpis(
    decimal TodayIncome,
    decimal TodayExpense,
    int OpenRegisters
);

public sealed record HrKpis(
    int ActiveEmployees,
    int PendingVacations,
    int ActivePermissions,
    int TotalEmployees
);
