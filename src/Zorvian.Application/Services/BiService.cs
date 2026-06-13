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
        var todaySales = await _saleRepo.GetTodaySalesAsync(NullBranch);
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var yesterdaySales = await _saleRepo.GetFilteredAsync(
            null, null, null, yesterday, yesterday.AddDays(1), null, NullBranch, 1, int.MaxValue);
        var yesterdayTotal = yesterdaySales.Sum(s => CurrencyConverter.ToReporting(s.Total, s.CurrencyCode, s.ExchangeRateToReporting, cc));
        var salesChange = yesterdayTotal > 0 ? (double)((todaySales - yesterdayTotal) / yesterdayTotal) * 100 : 0;

        var lastMonthSales = await _saleRepo.GetFilteredAsync(
            null, null, null,
            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1),
            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            null, NullBranch, 1, int.MaxValue);
        var lastMonthTotal = lastMonthSales.Sum(s => CurrencyConverter.ToReporting(s.Total, s.CurrencyCode, s.ExchangeRateToReporting, cc));
        var monthSales = await _saleRepo.GetMonthSalesAsync(NullBranch);
        var monthChange = lastMonthTotal > 0 ? (double)((monthSales - lastMonthTotal) / lastMonthTotal) * 100 : 0;

        var avgTicket = await _saleRepo.GetAverageTicketAsync(NullBranch);
        var todayCount = await _saleRepo.GetTodaySalesCountAsync(NullBranch);

        var weekStart = DateTime.UtcNow.Date.AddDays(-6);
        var weekSales = new List<decimal>();
        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var daySales = await _saleRepo.GetFilteredAsync(
                null, null, null, day, day.AddDays(1), null, NullBranch, 1, int.MaxValue);
            weekSales.Add(daySales.Sum(s => CurrencyConverter.ToReporting(s.Total, s.CurrencyCode, s.ExchangeRateToReporting, cc)));
        }

        var activeCredits = await _creditRepo.GetActiveCreditsCountAsync(NullBranch);
        var overdueCredits = await _creditRepo.GetOverdueCreditsCountAsync(NullBranch);
        var monthlyRecovery = await _creditRepo.GetMonthlyRecoveryAsync(NullBranch);
        var totalPortfolio = await _creditRepo.GetTotalPortfolioAsync(NullBranch);
        var collectionRate = totalPortfolio > 0 ? (double)(monthlyRecovery / totalPortfolio) * 100 : 0;
        var overdueInstallments = await _creditRepo.GetOverdueInstallmentsAsync(NullBranch);
        var avgOverdueDays = overdueInstallments.Any()
            ? overdueInstallments.Average(i => (DateTime.UtcNow - i.DueDate.ToDateTime(TimeOnly.MinValue)).TotalDays)
            : 0;
        var monthSalesCount = await _saleRepo.GetFilteredCountAsync(
            null, null, null,
            new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1),
            DateTime.UtcNow, null, NullBranch);
        var avgMonthlySales = monthSalesCount > 0 ? monthSales / monthSalesCount : 1;
        var dso = avgMonthlySales > 0 ? (double)(totalPortfolio / avgMonthlySales) * 30 : 0;

        var outOfStock = await _productRepo.GetOutOfStockAsync(NullBranch);
        var lowStock = await _productRepo.GetLowStockAsync(NullBranch);
        var totalProducts = await _productRepo.GetTotalCountAsync(NullBranch);
        var allProducts = await _productRepo.GetFilteredAsync(null, null, null, null, null, NullBranch, 1, int.MaxValue);
        var totalStockValue = allProducts.Sum(p => p.CostPrice * p.Stock);
        var turnoverRate = allProducts.Any() ? (double)allProducts.Sum(p => p.Stock) / allProducts.Count : 0;
        var topSelling = await _productRepo.GetTopSellingAsync(NullBranch, 5);

        var todayIncome = await _cashRepo.GetTodayIncomeAsync(NullBranch);
        var todayExpense = await _cashRepo.GetTodayExpenseAsync(NullBranch);
        var openRegisters = await _cashRepo.GetOpenRegistersCountAsync(NullBranch);

        var activeEmployees = await _dashRepo.GetActiveEmployeesAsync();
        var totalEmployees = await _dashRepo.GetTotalEmployeesAsync();
        var payrollHistory = await _dashRepo.GetPayrollHistoryAsync(1);
        var payrollTotal = payrollHistory.Any() ? payrollHistory.Sum(h => h.Amount) : 0;
        var avgCostPerEmp = activeEmployees > 0 ? payrollTotal / activeEmployees : 0;
        var pendingVac = await _dashRepo.GetPendingVacationRequestsAsync();
        var pendingPerm = await _dashRepo.GetPendingPermissionRequestsAsync();

        var alerts = new List<BiAlertItem>();
        if (outOfStock.Count > 0)
            alerts.Add(new("inventory", $"{outOfStock.Count} productos sin stock", "high"));
        if (overdueCredits > 0)
            alerts.Add(new("credit", $"{overdueCredits} créditos vencidos", "high"));
        if (lowStock.Count > 0)
            alerts.Add(new("inventory", $"{lowStock.Count} productos con stock bajo", "medium"));

        return new BiExecutiveResponse(
            new BiSalesKpi(todaySales, yesterdayTotal, salesChange, monthSales, monthChange, avgTicket, todayCount, weekSales),
            new BiCreditKpi(activeCredits, overdueCredits, monthlyRecovery, totalPortfolio, collectionRate, Math.Round(dso, 1)),
            new BiInventoryKpi(outOfStock.Count, lowStock.Count, totalProducts, totalStockValue, Math.Round(turnoverRate, 2),
                topSelling.Select(p => new BiTopProductItem(p.Product.Name, p.TotalSold)).ToList()),
            new BiCashKpi(todayIncome, todayExpense, todayIncome - todayExpense, openRegisters),
            new BiHrKpi(activeEmployees, totalEmployees, payrollTotal, Math.Round(avgCostPerEmp, 2), pendingVac + pendingPerm),
            alerts
        );
    }

    public async Task<BiSalesTrendResponse> GetSalesTrendAsync(int months = 12)
    {
        var cc = await GetCompanyCurrencyAsync();
        var toDate = DateTime.UtcNow;
        var fromDate = toDate.AddMonths(-months);
        var sales = await _saleRepo.GetFilteredAsync(null, null, null, fromDate, toDate, null, NullBranch, 1, int.MaxValue);

        var monthly = sales
            .GroupBy(s => new { s.CreatedAt.Year, s.CreatedAt.Month })
            .Select(g => new BiMonthSales(
                g.Key.Year, g.Key.Month,
                g.Sum(s => CurrencyConverter.ToReporting(s.Total, s.CurrencyCode, s.ExchangeRateToReporting, cc)), g.Count(),
                g.Count() > 0 ? g.Sum(s => CurrencyConverter.ToReporting(s.Total, s.CurrencyCode, s.ExchangeRateToReporting, cc)) / g.Count() : 0,
                0, 0))
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

        var total = monthly.Sum(m => m.Total);
        var avg = monthly.Any() ? total / monthly.Count : 0;
        var change = monthly.Count >= 2
            ? (double)((monthly.Last().Total - monthly.First().Total) / (monthly.First().Total > 0 ? monthly.First().Total : 1)) * 100
            : 0;

        return new BiSalesTrendResponse(monthly, Math.Round(change, 1), total, Math.Round(avg, 2));
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
        var credits = await _creditRepo.GetFilteredAsync(null, null, null, NullBranch, 1, int.MaxValue);
        var openCredits = credits.Where(c => c.Status == "active" || c.Status == "overdue").ToList();
        var totalPortfolio = openCredits.Sum(c => CurrencyConverter.ToReporting(c.Balance, c.CurrencyCode, c.ExchangeRateToReporting, cc));
        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var current = 0m; var d30 = 0m; var d60 = 0m; var d90 = 0m; var d90p = 0m;
        var byClient = new List<BiClientAgingItem>();

        foreach (var credit in openCredits)
        {
            var installments = await _creditRepo.GetInstallmentsByCreditIdAsync(credit.Id);
            var pendingInst = installments.Where(i => i.Status == "pending" || i.Status == "overdue").ToList();

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

        var entries = await _entryRepo.GetFilteredAsync(period.Id, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var allDetails = new List<AccountingEntryDetail>();
        foreach (var e in entries)
        {
            var full = await _entryRepo.GetByIdAsync(e.Id);
            if (full != null) allDetails.AddRange(full.Details);
        }

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
        var allProducts = await _productRepo.GetFilteredAsync(null, null, null, null, null, NullBranch, 1, int.MaxValue);
        var lowStock = await _productRepo.GetLowStockAsync(NullBranch);
        var outOfStock = await _productRepo.GetOutOfStockAsync(NullBranch);

        var totalValue = allProducts.Sum(p => p.CostPrice * p.Stock);
        var totalProducts = allProducts.Count;
        var turnoverRate = allProducts.Any() ? (double)allProducts.Sum(p => p.Stock) / allProducts.Count : 0;

        var byCategory = allProducts
            .GroupBy(p => p.Category?.Name ?? "Sin categoría")
            .Select(g => new BiCategoryInventoryItem(
                g.Key, g.Count(),
                g.Sum(p => p.CostPrice * p.Stock),
                g.Sum(p => p.SellingPrice * p.Stock)))
            .OrderByDescending(c => c.TotalCost)
            .ToList();

        var slowMovers = allProducts
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.InventoryMovements?.Any() == true
                ? p.InventoryMovements.Max(m => m.CreatedAt)
                : DateTime.MinValue)
            .Take(10)
            .Select(p => new BiSlowMoverItem(
                p.Name, p.Stock,
                p.InventoryMovements?.Any() == true
                    ? (int)(DateTime.UtcNow - p.InventoryMovements.Max(m => m.CreatedAt)).TotalDays / 30
                    : 99))
            .ToList();

        return new BiInventorySummaryResponse(
            totalValue, totalProducts, lowStock.Count, outOfStock.Count,
            Math.Round(turnoverRate, 2), 0, byCategory, slowMovers
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
        var posted = await _entryRepo.GetFilteredAsync(periodId, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var allEntries = await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id)));
        var details = allEntries.Where(e => e != null).SelectMany(e => e!.Details).ToList();
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
        var entries = await _entryRepo.GetFilteredAsync(null, null, "posted", null, period.OpenedAt ?? period.CreatedAt, CompanyId, 1, int.MaxValue);
        var cashAccounts = await _accountRepo.GetByCodesAsync(["1.1.01", "1.1.02"], CompanyId);
        var cashIds = cashAccounts.Select(a => a.Id).ToHashSet();
        var balance = 0m;
        foreach (var e in entries)
        {
            var full = await _entryRepo.GetByIdAsync(e.Id);
            if (full != null)
            {
                var cashDetail = full.Details.FirstOrDefault(d => cashIds.Contains(d.AccountId));
                if (cashDetail != null)
                    balance += cashDetail.DebitAmount - cashDetail.CreditAmount;
            }
        }
        return balance;
    }

    private static BiLineItemChange NewChange(string name, decimal current, decimal previous) =>
        new(name, current, previous, previous > 0 ? Math.Round((double)((current - previous) / previous) * 100, 1) : 0);
}
