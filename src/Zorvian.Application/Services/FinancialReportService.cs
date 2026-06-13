using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class FinancialReportService
{
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ICostCenterRepository _costCenterRepo;
    private readonly IBudgetRepository _budgetRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;
    private string? _companyCurrencyCache;

    public FinancialReportService(
        IAccountingEntryRepository entryRepo,
        IAccountingPeriodRepository periodRepo,
        IAccountRepository accountRepo,
        ICostCenterRepository costCenterRepo,
        IBudgetRepository budgetRepo,
        ICompanyRepository companyRepo,
        ITenantContext tenant)
    {
        _entryRepo = entryRepo; _periodRepo = periodRepo;
        _accountRepo = accountRepo; _costCenterRepo = costCenterRepo;
        _budgetRepo = budgetRepo;
        _companyRepo = companyRepo;
        _tenant = tenant;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    private async Task<string> GetCompanyCurrencyAsync()
    {
        if (_companyCurrencyCache != null) return _companyCurrencyCache;
        var company = await _companyRepo.GetByIdAsync(CompanyId);
        _companyCurrencyCache = company?.Currency ?? "NIO";
        return _companyCurrencyCache;
    }

    private static decimal Convert(decimal amount, AccountingEntry entry, string cc) =>
        CurrencyConverter.ToReporting(amount, entry.CurrencyCode, entry.ExchangeRateToReporting, cc);

    private static decimal ConvertSum(IEnumerable<AccountingEntryDetail> details, IReadOnlyDictionary<Guid, AccountingEntry> entryMap, Func<AccountingEntryDetail, decimal> selector, string cc) =>
        details.Sum(d => Convert(selector(d), entryMap[d.AccountingEntryId], cc));

    public async Task<TrialBalanceResponse?> GetTrialBalanceAsync(Guid periodId)
    {
        var cc = await GetCompanyCurrencyAsync();
        var period = await _periodRepo.GetByIdAsync(periodId);
        if (period is null) return null;
        if (period.CompanyId != CompanyId)
            throw new KeyNotFoundException("Period not found for this company");

        var posted = await _entryRepo.GetFilteredAsync(periodId, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var allEntries = (await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>().ToList();
        var entryMap = allEntries.ToDictionary(e => e.Id);
        var details = allEntries.SelectMany(e => e.Details).ToList();

        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var leafAccounts = accounts.Where(a => a.Level >= 2 && a.IsActive).ToList();

        var items = leafAccounts.Select(a =>
        {
            var relatedDetails = details.Where(d => d.AccountId == a.Id).ToList();
            var debitMovements = ConvertSum(relatedDetails, entryMap, d => d.DebitAmount, cc);
            var creditMovements = ConvertSum(relatedDetails, entryMap, d => d.CreditAmount, cc);
            var openingBalance = a.OpeningBalance;
            var endingBalance = a.NormalSide == "Debit"
                ? openingBalance + debitMovements - creditMovements
                : openingBalance + creditMovements - debitMovements;

            return new TrialBalanceItem(a.Code, a.Name, a.Type, openingBalance, debitMovements, creditMovements, endingBalance);
        }).ToList();

        return new TrialBalanceResponse(
            periodId, period.Name, DateTime.UtcNow,
            items.Sum(i => i.OpeningBalance), items.Sum(i => i.OpeningBalance),
            items.Sum(i => i.DebitMovements), items.Sum(i => i.CreditMovements),
            items.Sum(i => i.EndingBalance), items.Sum(i => i.EndingBalance),
            items
        );
    }

    public async Task<IncomeStatementResponse?> GetIncomeStatementAsync(Guid periodId)
    {
        var trialBalance = await GetTrialBalanceAsync(periodId);
        if (trialBalance is null) return null;
        var period = await _periodRepo.GetByIdAsync(periodId);

        var income = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Income).Sum(i => i.EndingBalance);
        var cost = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Cost).Sum(i => i.EndingBalance);
        var expenses = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Expense).Sum(i => i.EndingBalance);
        var grossProfit = income - cost;
        var netIncome = grossProfit - expenses;

        return new IncomeStatementResponse(
            period?.Name ?? "", income, cost, grossProfit, expenses, netIncome
        );
    }

    public async Task<BalanceSheetResponse?> GetBalanceSheetAsync(Guid periodId)
    {
        var trialBalance = await GetTrialBalanceAsync(periodId);
        if (trialBalance is null) return null;
        var period = await _periodRepo.GetByIdAsync(periodId);

        var assets = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Asset).ToList();
        var liabilities = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Liability).ToList();
        var equity = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Equity).ToList();

        // Add net income to retained earnings
        var incomeSt = await GetIncomeStatementAsync(periodId);
        var netIncome = incomeSt?.NetIncome ?? 0;
        var retainedEarnings = equity.FirstOrDefault(e => e.AccountCode == "3.1.02");
        if (retainedEarnings != null && netIncome != 0)
        {
            equity.Remove(retainedEarnings);
            equity.Add(retainedEarnings with { EndingBalance = retainedEarnings.EndingBalance + netIncome });
        }

        var sections = new List<BalanceSheetSection>
        {
            new("Activos", "Asset", assets.Select(a => new BalanceSheetItem(a.AccountCode, a.AccountName, a.EndingBalance)).ToList()),
            new("Pasivos", "Liability", liabilities.Select(l => new BalanceSheetItem(l.AccountCode, l.AccountName, l.EndingBalance)).ToList()),
            new("Patrimonio", "Equity", equity.Select(e => new BalanceSheetItem(e.AccountCode, e.AccountName, e.EndingBalance)).ToList()),
        };

        return new BalanceSheetResponse(
            period?.Name ?? "",
            assets.Sum(a => a.EndingBalance),
            liabilities.Sum(l => l.EndingBalance),
            equity.Sum(e => e.EndingBalance),
            sections
        );
    }

    public async Task<GeneralLedgerResponse?> GetGeneralLedgerAsync(Guid accountId, DateTime? fromDate, DateTime? toDate)
    {
        var cc = await GetCompanyCurrencyAsync();
        var account = await _accountRepo.GetByIdAsync(accountId);
        if (account is null) return null;

        var posted = await _entryRepo.GetFilteredAsync(null, null, "posted", fromDate, toDate, CompanyId, 1, int.MaxValue);
        var allEntries = (await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>()
            .OrderBy(e => e.EntryDate)
            .ToList();

        var openingBalance = account.OpeningBalance;
        var runningBalance = openingBalance;
        var isDebitNormal = account.NormalSide == "Debit";

        var items = new List<GeneralLedgerItem>();
        foreach (var entry in allEntries.Where(e => e.Details.Any(d => d.AccountId == accountId)))
        {
            var detail = entry.Details.First(d => d.AccountId == accountId);
            var debit = Convert(detail.DebitAmount, entry, cc);
            var credit = Convert(detail.CreditAmount, entry, cc);
            runningBalance += isDebitNormal ? debit - credit : credit - debit;

            items.Add(new GeneralLedgerItem(
                entry.EntryDate, entry.EntryNumber, entry.Description, entry.ReferenceType,
                debit, credit, runningBalance
            ));
        }

        return new GeneralLedgerResponse(
            account.Code, account.Name, openingBalance,
            items.Sum(i => i.DebitAmount), items.Sum(i => i.CreditAmount), runningBalance,
            items
        );
    }

    public async Task<CostCenterExpenseReport> GetCostCenterExpenseReportAsync(Guid costCenterId, DateTime? fromDate, DateTime? toDate)
    {
        var cc = await GetCompanyCurrencyAsync();
        var posted = await _entryRepo.GetFilteredAsync(null, null, "posted", fromDate, toDate, CompanyId, 1, int.MaxValue);
        var allEntries = (await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>().ToList();
        var entryMap = allEntries.ToDictionary(e => e.Id);
        var relevantDetails = allEntries
            .SelectMany(e => e.Details)
            .Where(d => d.CostCenterId == costCenterId)
            .ToList();

        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var expenseAccounts = accounts.Where(a => a.IsActive && (a.Type == AccountTypes.Expense || a.Type == AccountTypes.Cost)).ToList();

        var costCenter = await _costCenterRepo.GetByIdAsync(costCenterId);
        var costCenterName = costCenter?.Name ?? "";
        var costCenterCode = costCenter?.Code ?? "";

        var items = expenseAccounts.Select(a =>
        {
            var accountDetails = relevantDetails.Where(d => d.AccountId == a.Id).ToList();
            var debit = ConvertSum(accountDetails, entryMap, d => d.DebitAmount, cc);
            var credit = ConvertSum(accountDetails, entryMap, d => d.CreditAmount, cc);
            var balance = a.NormalSide == "Debit" ? debit - credit : credit - debit;
            return new CostCenterExpenseItem(a.Code, a.Name, debit, credit, balance);
        }).Where(i => i.Balance != 0).ToList();

        return new CostCenterExpenseReport(
            costCenterId, costCenterName, costCenterCode,
            DateTime.UtcNow,
            items.Sum(i => i.DebitAmount),
            items.Sum(i => i.CreditAmount),
            items.Sum(i => i.Balance),
            items
        );
    }

    public async Task<BudgetVsActualReport> GetBudgetVsActualAsync(int year, int month)
    {
        var cc = await GetCompanyCurrencyAsync();
        var fromDate = new DateTime(year, month, 1);
        var toDate = fromDate.AddMonths(1).AddDays(-1);

        var budgets = await _budgetRepo.GetByPeriodAsync(year, month, CompanyId);
        var posted = await _entryRepo.GetFilteredAsync(null, null, "posted", fromDate, toDate, CompanyId, 1, int.MaxValue);
        var allEntries = (await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>().ToList();
        var entryMap = allEntries.ToDictionary(e => e.Id);
        var details = allEntries.SelectMany(e => e.Details).ToList();

        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var accountLookup = accounts.ToDictionary(a => a.Id);

        var items = budgets.Select(b =>
        {
            var accountDetails = details.Where(d => d.AccountId == b.AccountId
                && (!b.CostCenterId.HasValue || d.CostCenterId == b.CostCenterId)).ToList();
            var debit = ConvertSum(accountDetails, entryMap, d => d.DebitAmount, cc);
            var credit = ConvertSum(accountDetails, entryMap, d => d.CreditAmount, cc);
            var normalSide = accountLookup.TryGetValue(b.AccountId, out var acc) ? acc.NormalSide : "Debit";
            var actual = normalSide == "Debit" ? debit - credit : credit - debit;
            var variance = actual - b.BudgetedAmount;
            var variancePercent = b.BudgetedAmount != 0 ? Math.Round(variance / b.BudgetedAmount * 100, 2) : 0;

            return new BudgetVsActualItem(
                b.Id, b.Year, b.Month,
                b.AccountId, b.Account.Code, b.Account.Name,
                b.CostCenterId, b.CostCenter?.Name,
                b.BudgetedAmount, actual, variance, variancePercent
            );
        }).ToList();

        return new BudgetVsActualReport(
            year, month, DateTime.UtcNow,
            items.Sum(i => i.BudgetedAmount),
            items.Sum(i => i.ActualAmount),
            items.Sum(i => i.Variance),
            items
        );
    }
}
