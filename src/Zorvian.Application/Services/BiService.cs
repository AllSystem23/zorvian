using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Bi;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Enums;
using Zorvian.Core.Interfaces;


namespace Zorvian.Application.Services;

public sealed class BiService
{
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly ICreditRepository _creditRepo;
    private readonly IProductRepository _productRepo;
    private readonly ICashRegisterRepository _cashRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IClientRepository _clientRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IQuoteRepository _quoteRepo;
    private readonly IDashboardRepository _dashRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;
    private string? _companyCurrencyCache;

    public BiService(
        ISaleRepository saleRepo, IPurchaseRepository purchaseRepo,
        ICreditRepository creditRepo, IProductRepository productRepo,
        ICashRegisterRepository cashRepo, IAccountRepository accountRepo,
        IAccountingEntryRepository entryRepo, IAccountingPeriodRepository periodRepo,
        IClientRepository clientRepo, ISupplierRepository supplierRepo,
        IQuoteRepository quoteRepo, IDashboardRepository dashRepo,
        ICompanyRepository companyRepo, ITenantContext tenant)
    {
        _saleRepo = saleRepo; _purchaseRepo = purchaseRepo;
        _creditRepo = creditRepo; _productRepo = productRepo;
        _cashRepo = cashRepo; _accountRepo = accountRepo;
        _entryRepo = entryRepo; _periodRepo = periodRepo;
        _clientRepo = clientRepo; _supplierRepo = supplierRepo;
        _quoteRepo = quoteRepo; _dashRepo = dashRepo;
        _companyRepo = companyRepo; _tenant = tenant;
    }

    private Guid CompanyId =>
        Guid.TryParse(_tenant.TenantId, out var id) ? id : Guid.Empty;
    private Guid NullBranch => Guid.Empty;

    private async Task<string> GetCompanyCurrencyAsync()
    {
        if (_companyCurrencyCache != null) return _companyCurrencyCache;
        var company = await _companyRepo.GetByIdAsync(CompanyId);
        _companyCurrencyCache = company?.Currency ?? "NIO";
        return _companyCurrencyCache;
    }

    public async Task<BiExecutiveResponse> GetExecutiveAsync()
    {
        var cc = await GetCompanyCurrencyAsync();
        var tenantId = _tenant.TenantId.Value.ToString();
        var isSuperAdmin = _tenant.IsSuperAdmin;

        // Phase 1: High-performance scalar KPIs (2 round-trips for everything)
        var execScalarsTask = _dashRepo.GetExecutiveKpiScalarsRawAsync(_tenant.EffectiveCompanyId, _tenant.IsSuperAdmin);
        var thirtyDaysAgo = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var now = DateTime.UtcNow;
        var lastMonth = now.Month == 1 ? 12 : now.Month - 1;
        var hrScalarsTask = _dashRepo.GetAllKpiScalarsRawAsync(_tenant.EffectiveCompanyId, _tenant.IsSuperAdmin, thirtyDaysAgo, now.Month, lastMonth);

        var scalars = await execScalarsTask;
        var hrScalars = await hrScalarsTask;

        // Phase 2: Sequential EF queries for lists/complex logic
        var topSelling = await _productRepo.GetTopSellingAsync(Guid.Empty, 5);
        
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var todayStart = DateTime.UtcNow.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);
        var weekStart = todayStart.AddDays(-6);
        var salesMetrics = await _saleRepo.GetExecutiveSalesMetricsRawAsync(
            lastMonthStart, monthStart, todayStart, yesterday, weekStart, todayStart.AddDays(1), NullBranch, cc);

        var collectionRate = scalars.TotalPortfolio > 0 ? (double)(scalars.MonthlyRecovery / scalars.TotalPortfolio) * 100 : 0;
        
        // This is still heavy but much better than 20+ separate counts
        var turnoverRate = scalars.TotalProducts > 0 ? (double)scalars.TotalProducts / 1.0 : 0; // Estimation

        var payrollHistory = await _dashRepo.GetPayrollHistoryAsync(1);
        var payrollTotal = payrollHistory.Any() ? payrollHistory.Sum(h => h.Amount) : 0;
        var avgCostPerEmp = scalars.ActiveEmployees > 0 ? payrollTotal / scalars.ActiveEmployees : 0;

        var alerts = new List<BiAlertItem>();
        if (scalars.OutOfStockCount > 0)
            alerts.Add(new("inventory", $"{scalars.OutOfStockCount} productos sin stock", "high"));
        if (scalars.OverdueCredits > 0)
            alerts.Add(new("credit", $"{scalars.OverdueCredits} créditos vencidos", "high"));
        if (scalars.LowStockCount > 0)
            alerts.Add(new("inventory", $"{scalars.LowStockCount} productos con stock bajo", "medium"));

        return new BiExecutiveResponse(
            new BiSalesKpi(salesMetrics.TodaySales, salesMetrics.YesterdaySales, salesMetrics.SalesChangePercent, salesMetrics.MonthSales, salesMetrics.MonthSalesChangePercent, salesMetrics.AverageTicket, salesMetrics.TodaySalesCount, salesMetrics.WeeklyTrend),
            new BiCreditKpi(scalars.ActiveCredits, scalars.OverdueCredits, scalars.MonthlyRecovery, scalars.TotalPortfolio, collectionRate, 0),
            new BiInventoryKpi(scalars.OutOfStockCount, scalars.LowStockCount, scalars.TotalProducts, 0, Math.Round(turnoverRate, 2),
                topSelling.Select(p => new BiTopProductItem(p.Product?.Name ?? "Producto eliminado", p.TotalSold)).ToList()),
            new BiCashKpi(scalars.TodayIncome, scalars.TodayExpense, scalars.TodayIncome - scalars.TodayExpense, scalars.OpenRegisters),
            new BiHrKpi(scalars.ActiveEmployees, scalars.TotalEmployees, payrollTotal, Math.Round(avgCostPerEmp, 2), scalars.PendingVacations + scalars.PendingPermissions),
            alerts
        );
    }

    public async Task<BiSalesTrendResponse> GetSalesTrendAsync(int months = 12)
    {
        var cc = await GetCompanyCurrencyAsync();
        var toDate = DateTime.UtcNow;
        var fromDate = toDate.AddMonths(-months);
        var monthly = await _saleRepo.GetSalesTrendRawAsync(fromDate, toDate, NullBranch, cc);

        var total = monthly.Sum(m => m.Total);
        var avg = monthly.Any() ? total / monthly.Count : 0;
        var change = monthly.Count >= 2
            ? (double)((monthly.Last().Total - monthly.First().Total) / (monthly.First().Total > 0 ? monthly.First().Total : 1)) * 100
            : 0;

        return new BiSalesTrendResponse(monthly.Select(m => new BiMonthSales(
            m.Year, m.Month, m.Total, m.Count, m.AverageTicket, 0, 0)).ToList(), Math.Round(change, 1), total, Math.Round(avg, 2));
    }

    public Task<BiSalesByCategoryResponse> GetSalesByCategoryAsync(DateTime? from, DateTime? to)
    {
        return Task.FromResult(new BiSalesByCategoryResponse(new List<BiCategorySalesItem>()));
    }

    public Task<List<BiSalesBySellerItem>> GetSalesBySellerAsync(DateTime? from, DateTime? to)
    {
        return Task.FromResult(new List<BiSalesBySellerItem>());
    }

    public async Task<BiQuoteConversionResponse> GetQuoteConversionAsync(DateTime? from, DateTime? to)
    {
        from ??= DateTime.UtcNow.AddMonths(-3);
        to ??= DateTime.UtcNow;
        var quotes = await _quoteRepo.GetFilteredAsync(null, null, from, to, null, NullBranch, 1, int.MaxValue);
        var converted = quotes.Where(q => q.Status == QuoteStatus.Accepted).ToList();
        var total = quotes.Count;
        var rate = total > 0 ? (double)converted.Count / total * 100 : 0;
        var avgDays = converted.Any()
            ? converted.Average(q => (q.UpdatedAt.HasValue ? (q.UpdatedAt.Value - q.CreatedAt).TotalDays : 0))
            : 0;

        return new BiQuoteConversionResponse(total, converted.Count, Math.Round(rate, 1), Math.Round(avgDays, 1));
    }

    public async Task<BiArAgingResponse> GetArAgingAsync()
    {
        var cc = await GetCompanyCurrencyAsync();
        var openCredits = (await _creditRepo.GetFilteredAsync(null, null, null, NullBranch, 1, int.MaxValue))
            .Where(c => !c.IsDeleted && (c.Status == "active" || c.Status == "overdue"))
            .ToList();
        var totalPortfolio = openCredits.Sum(c => CurrencyConverter.ToReporting(c.Balance, c.CurrencyCode, c.ExchangeRateToReporting, cc));
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var current = 0m; var d30 = 0m; var d60 = 0m; var d90 = 0m; var d90p = 0m;
        var byClient = new List<BiClientAgingItem>();

        foreach (var credit in openCredits)
        {
            var pendingInst = credit.Installments
                .Where(i => !i.IsDeleted && (i.Status == "pending" || i.Status == "overdue"))
                .ToList();

            var cCur = 0m; var c30 = 0m; var c60 = 0m; var c90 = 0m; var c90p = 0m;

            foreach (var inst in pendingInst)
            {
                var amt = CurrencyConverter.ToReporting(inst.Amount, credit.CurrencyCode, credit.ExchangeRateToReporting, cc);
                var daysOverdue = (now.ToDateTime(TimeOnly.MinValue) - inst.DueDate.ToDateTime(TimeOnly.MinValue)).Days;
                if (daysOverdue <= 0) { cCur += amt; current += amt; }
                else if (daysOverdue <= 30) { c30 += amt; d30 += amt; }
                else if (daysOverdue <= 60) { c60 += amt; d60 += amt; }
                else if (daysOverdue <= 90) { c90 += amt; d90 += amt; }
                else { c90p += amt; d90p += amt; }
            }

            var clientTotal = cCur + c30 + c60 + c90 + c90p;
            if (credit.Client != null && clientTotal > 0)
            {
                byClient.Add(new BiClientAgingItem(
                    $"{credit.Client.FirstName} {credit.Client.LastName}", clientTotal,
                    cCur, c30, c60, c90, c90p));
            }
        }

        var totalOverdue = d30 + d60 + d90 + d90p;
        var overduePct = totalPortfolio > 0 ? (double)totalOverdue / (double)totalPortfolio * 100 : 0;

        return new BiArAgingResponse(
            current, d30, d60, d90, d90p,
            totalOverdue, totalPortfolio, Math.Round(overduePct, 1),
            byClient.OrderByDescending(c => c.Balance).Take(10).ToList()
        );
    }

    public async Task<BiApAgingResponse> GetApAgingAsync()
    {
        var cc = await GetCompanyCurrencyAsync();
        var purchases = await _purchaseRepo.GetFilteredAsync(null, null, null, null, null, NullBranch, 1, int.MaxValue);
        var pending = purchases.Where(p => p.Status == "pending" || p.Status == "completed").ToList();
        var totalPayable = pending.Sum(p => CurrencyConverter.ToReporting(p.Balance, p.CurrencyCode, p.ExchangeRateToReporting, cc));
        var now = DateTime.UtcNow;

        var current = 0m; var d30 = 0m; var d60 = 0m; var d90 = 0m; var d90p = 0m;
        var bySupplier = new List<BiSupplierAgingItem>();

        foreach (var purchase in pending.Where(p => p.Balance > 0))
        {
            var bal = CurrencyConverter.ToReporting(purchase.Balance, purchase.CurrencyCode, purchase.ExchangeRateToReporting, cc);
            if (purchase.DueDate == null) { current += bal; continue; }
            var due = purchase.DueDate.Value.ToDateTime(TimeOnly.MinValue);
            var daysOverdue = (now - due).Days;

            var sCur = 0m; var s30 = 0m; var s60 = 0m; var s90 = 0m; var s90p = 0m;

            if (daysOverdue <= 0) { sCur = bal; current += bal; }
            else if (daysOverdue <= 30) { s30 = bal; d30 += bal; }
            else if (daysOverdue <= 60) { s60 = bal; d60 += bal; }
            else if (daysOverdue <= 90) { s90 = bal; d90 += bal; }
            else { s90p = bal; d90p += bal; }

            if (purchase.Supplier != null)
            {
                bySupplier.Add(new BiSupplierAgingItem(
                    purchase.Supplier.Name, bal, sCur, s30, s60, s90, s90p));
            }
        }

        var totalOverdue = d30 + d60 + d90 + d90p;
        var overduePct = totalPayable > 0 ? (double)totalOverdue / (double)totalPayable * 100 : 0;

        return new BiApAgingResponse(
            current, d30, d60, d90, d90p,
            totalOverdue, totalPayable, Math.Round(overduePct, 1),
            bySupplier.OrderByDescending(s => s.Balance).Take(10).ToList()
        );
    }

    public async Task<BiFinancialRatiosResponse> GetFinancialRatiosAsync(Guid? periodId)
    {
        AccountingPeriod? period = null;
        if (periodId.HasValue)
            period = await _periodRepo.GetByIdAsync(periodId.Value);
        period ??= await _periodRepo.GetCurrentOpenAsync(CompanyId)
            ?? (await _periodRepo.GetAllAsync(CompanyId)).OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).FirstOrDefault();

        if (period == null) return new BiFinancialRatiosResponse(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        var tb = await GetTrialBalanceAsync(period.Id);

        var assets = tb.Where(a => a.AccountType == AccountTypes.Asset).Sum(a => a.EndingBalance);
        var currentAssets = tb.Where(a => a.AccountType == AccountTypes.Asset && a.AccountCode.StartsWith("1.1")).Sum(a => a.EndingBalance);
        var liabilities = tb.Where(a => a.AccountType == AccountTypes.Liability).Sum(a => a.EndingBalance);
        var currentLiabilities = tb.Where(a => a.AccountType == AccountTypes.Liability && a.AccountCode.StartsWith("2.1")).Sum(a => a.EndingBalance);
        var equity = tb.Where(a => a.AccountType == AccountTypes.Equity).Sum(a => a.EndingBalance);
        var inventory = tb.Where(a => a.AccountCode == "1.1.04").Sum(a => a.EndingBalance);
        var income = tb.Where(a => a.AccountType == AccountTypes.Income).Sum(a => a.EndingBalance);
        var cost = tb.Where(a => a.AccountType == AccountTypes.Cost).Sum(a => a.EndingBalance);
        var expenses = tb.Where(a => a.AccountType == AccountTypes.Expense).Sum(a => a.EndingBalance);

        var netIncome = income - cost - expenses;
        var grossProfit = income - cost;

        var currentRatio = currentLiabilities > 0 ? Math.Round((double)currentAssets / (double)currentLiabilities, 2) : 0;
        var quickRatio = currentLiabilities > 0 ? Math.Round((double)(currentAssets - inventory) / (double)currentLiabilities, 2) : 0;
        var debtRatio = assets > 0 ? Math.Round((double)liabilities / (double)assets, 2) : 0;
        var debtToEquity = equity > 0 ? Math.Round((double)liabilities / (double)equity, 2) : 0;
        var grossMargin = income > 0 ? Math.Round((double)grossProfit / (double)income * 100, 1) : 0;
        var netMargin = income > 0 ? Math.Round((double)netIncome / (double)income * 100, 1) : 0;
        var operatingMargin = income > 0 ? Math.Round((double)(grossProfit - expenses) / (double)income * 100, 1) : 0;
        var roa = assets > 0 ? Math.Round((double)netIncome / (double)assets * 100, 1) : 0;
        var roe = equity > 0 ? Math.Round((double)netIncome / (double)equity * 100, 1) : 0;
        var workingCapital = currentAssets - currentLiabilities;
        var breakEven = grossMargin > 0 ? Math.Round((double)expenses / ((double)grossMargin / 100), 2) : 0;

        return new BiFinancialRatiosResponse(
            currentRatio, quickRatio, debtRatio, debtToEquity,
            grossMargin, netMargin, operatingMargin, roa, roe,
            workingCapital, (decimal)breakEven
        );
    }

    public async Task<BiComparativeIncomeResponse> GetComparativeIncomeAsync(Guid? periodId1, Guid? periodId2)
    {
        if (!periodId1.HasValue)
        {
            var periods = (await _periodRepo.GetAllAsync(CompanyId))
                .OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).Take(2).ToList();
            periodId1 = periods.Count > 0 ? periods[0].Id : Guid.Empty;
            periodId2 = periods.Count > 1 ? periods[1].Id : Guid.Empty;
        }

        periodId2 ??= Guid.Empty;

        var current = await GetIncomeStatementAsync(periodId1.Value);
        var previous = periodId2.Value != Guid.Empty
            ? await GetIncomeStatementAsync(periodId2.Value)
            : new IncomeStatementResponse("", 0, 0, 0, 0, 0);

        var currentDto = new BiIncomeStatement(current.PeriodName, current.TotalIncome, current.TotalCost, current.GrossProfit, current.TotalExpenses, current.NetIncome);
        var previousDto = new BiIncomeStatement(previous.PeriodName, previous.TotalIncome, previous.TotalCost, previous.GrossProfit, previous.TotalExpenses, previous.NetIncome);

        var charges = new List<BiLineItemChange>
        {
            NewChange("Ingresos", current.TotalIncome, previous.TotalIncome),
            NewChange("Costos", current.TotalCost, previous.TotalCost),
            NewChange("Utilidad Bruta", current.GrossProfit, previous.GrossProfit),
            NewChange("Gastos", current.TotalExpenses, previous.TotalExpenses),
            NewChange("Utilidad Neta", current.NetIncome, previous.NetIncome),
        };

        return new BiComparativeIncomeResponse(currentDto, previousDto, charges);
    }

    public async Task<BiCashFlowResponse> GetCashFlowAsync(Guid? periodId)
    {
        AccountingPeriod? period = null;
        if (periodId.HasValue)
            period = await _periodRepo.GetByIdAsync(periodId.Value);
        period ??= await _periodRepo.GetCurrentOpenAsync(CompanyId);
        if (period == null) return new BiCashFlowResponse([], [], [], 0, 0, 0, DateTime.UtcNow);

        var entries = await _entryRepo.GetPostedWithDetailsAsync(period.Id, CompanyId);
        var allDetails = entries.SelectMany(e => e.Details).ToList();

        var cashAccounts = await _accountRepo.GetByCodesAsync(["1.1.01", "1.1.02"], CompanyId);
        var cashAccountIds = cashAccounts.Select(a => a.Id).ToHashSet();
        var cashDetails = allDetails.Where(d => cashAccountIds.Contains(d.AccountId)).ToList();

        var operating = new List<BiCashFlowItem>();
        var investing = new List<BiCashFlowItem>();
        var financing = new List<BiCashFlowItem>();

        foreach (var entry in entries)
        {
            var detail = entry.Details?.FirstOrDefault(d => cashAccountIds.Contains(d.AccountId));
            if (detail == null) continue;
            var netCash = detail.DebitAmount - detail.CreditAmount;
            if (netCash == 0) continue;

            var item = new BiCashFlowItem(entry.Description, netCash,
                netCash > 0 ? "inflow" : "outflow");

            if (entry.ReferenceType == "Sale" || entry.ReferenceType == "Purchase" ||
                entry.ReferenceType == "SupplierPayment" || entry.ReferenceType == "Payroll")
                operating.Add(item);
            else if (entry.ReferenceType == "FixedAssetAcquisition" || entry.ReferenceType == "FixedAssetDisposal")
                investing.Add(item);
            else
                operating.Add(item);
        }

        var beginningBalance = await GetInitialCashBalanceAsync(period.Id);
        var netOperating = operating.Sum(i => i.Amount);
        var netInvesting = investing.Sum(i => i.Amount);
        var netFinancing = financing.Sum(i => i.Amount);
        var netIncrease = netOperating + netInvesting + netFinancing;
        var endingBalance = beginningBalance + netIncrease;

        return new BiCashFlowResponse(
            operating, investing, financing,
            netIncrease, beginningBalance, endingBalance,
            DateTime.UtcNow
        );
    }

    public async Task<BiInventorySummaryResponse> GetInventorySummaryAsync()
    {
        var summary = await _productRepo.GetInventorySummaryRawAsync(NullBranch);
        var slowMovers = summary.TopSlowMovers
            .Select(p => new BiSlowMoverItem(
                p.ProductName,
                p.Stock,
                p.LastMovement == DateTime.MinValue ? 99 : Math.Max(0, (int)(DateTime.UtcNow - p.LastMovement).TotalDays / 30)))
            .ToList();

        return new BiInventorySummaryResponse(
            summary.TotalValue, summary.TotalProducts, summary.LowStockCount, summary.OutOfStockCount,
            summary.TurnoverRate, 0,
            summary.ByCategory.Select(c => new BiCategoryInventoryItem(
                c.CategoryName, c.Count, c.TotalCost, c.TotalValue)).ToList(),
            slowMovers
        );
    }

    public async Task<BiPayrollSummaryResponse> GetPayrollSummaryAsync(DateTime? from, DateTime? to)
    {
        from ??= new DateTime(DateTime.UtcNow.Year, 1, 1);
        to ??= DateTime.UtcNow;
        var costs = await _dashRepo.GetPayrollCostByDepartmentAsync();
        var history = await _dashRepo.GetPayrollHistoryAsync(12);

        var trend = history.Select(h => new BiPayrollTrendItem(
            h.Period, h.Amount, 0, h.Amount, 0)).ToList();

        var employees = await _dashRepo.GetActiveEmployeesAsync();
        var totalCost = history.Sum(h => h.Amount);
        var avgPerEmp = employees > 0 ? totalCost / employees : 0;

        var byDept = costs.Select(c => new BiPayrollCostByDept(
            c.Department, c.Amount, 0)).ToList();

        return new BiPayrollSummaryResponse(
            byDept, trend, totalCost, Math.Round(avgPerEmp, 2), 0, 0
        );
    }

    public async Task<BiEmployeeTurnoverResponse> GetEmployeeTurnoverAsync(DateTime? from, DateTime? to)
    {
        from ??= DateTime.UtcNow.AddMonths(-12);
        to ??= DateTime.UtcNow;
        var activeEmployees = await _dashRepo.GetActiveEmployeesAsync();

        // The dashboard repo doesn't have termination/hire counts by period,
        // so we estimate from current state
        var total = await _dashRepo.GetTotalEmployeesAsync();
        var active = await _dashRepo.GetActiveEmployeesAsync();
        var inactive = total - active;
        var rate = total > 0 ? (double)inactive / total * 100 : 0;

        var byDept = (await _dashRepo.GetEmployeesByDepartmentAsync())
            .Select(d => new BiTurnoverByDept(d.Name, 0, 0, 0))
            .ToList();

        return new BiEmployeeTurnoverResponse(0, inactive, Math.Round(rate, 1), active, byDept);
    }

    // -- private helpers --

    private async Task<List<TrialBalanceItem>> GetTrialBalanceAsync(Guid periodId)
    {
        var entries = await _entryRepo.GetPostedWithDetailsAsync(periodId, CompanyId);
        var details = entries.SelectMany(e => e.Details).ToList();
        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var leafAccounts = accounts.Where(a => a.Level >= 2 && a.IsActive).ToList();

        return leafAccounts.Select(a =>
        {
            var relatedDetails = details.Where(d => d.AccountId == a.Id).ToList();
            var debitMoves = relatedDetails.Sum(d => d.DebitAmount);
            var creditMoves = relatedDetails.Sum(d => d.CreditAmount);
            var ending = a.NormalSide == "Debit"
                ? a.OpeningBalance + debitMoves - creditMoves
                : a.OpeningBalance + creditMoves - debitMoves;
            return new TrialBalanceItem(a.Code, a.Name, a.Type, a.OpeningBalance, debitMoves, creditMoves, ending);
        }).ToList();
    }

    private async Task<IncomeStatementResponse> GetIncomeStatementAsync(Guid periodId)
    {
        var tb = await GetTrialBalanceAsync(periodId);
        var period = await _periodRepo.GetByIdAsync(periodId);
        var income = tb.Where(i => i.AccountType == AccountTypes.Income).Sum(i => i.EndingBalance);
        var cost = tb.Where(i => i.AccountType == AccountTypes.Cost).Sum(i => i.EndingBalance);
        var expenses = tb.Where(i => i.AccountType == AccountTypes.Expense).Sum(i => i.EndingBalance);
        return new IncomeStatementResponse(period?.Name ?? "", income, cost, income - cost, expenses, income - cost - expenses);
    }

    private async Task<decimal> GetInitialCashBalanceAsync(Guid periodId)
    {
        var period = await _periodRepo.GetByIdAsync(periodId);
        if (period == null) return 0;
        var entries = await _entryRepo.GetPostedWithDetailsAsync(null, CompanyId, period.OpenedAt ?? period.CreatedAt);
        var cashAccounts = await _accountRepo.GetByCodesAsync(["1.1.01", "1.1.02"], CompanyId);
        var cashIds = cashAccounts.Select(a => a.Id).ToHashSet();
        var balance = 0m;
        foreach (var e in entries)
        {
            var cashDetail = e.Details.FirstOrDefault(d => cashIds.Contains(d.AccountId));
            if (cashDetail != null)
                balance += cashDetail.DebitAmount - cashDetail.CreditAmount;
        }
        return balance;
    }

    private static BiLineItemChange NewChange(string name, decimal current, decimal previous) =>
        new(name, current, previous, previous > 0 ? Math.Round((double)((current - previous) / previous) * 100, 1) : 0);
}
