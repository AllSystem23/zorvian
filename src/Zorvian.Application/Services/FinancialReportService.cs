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
    private readonly ITenantContext _tenant;

    public FinancialReportService(
        IAccountingEntryRepository entryRepo,
        IAccountingPeriodRepository periodRepo,
        IAccountRepository accountRepo,
        ITenantContext tenant)
    {
        _entryRepo = entryRepo; _periodRepo = periodRepo;
        _accountRepo = accountRepo; _tenant = tenant;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<TrialBalanceResponse> GetTrialBalanceAsync(Guid periodId)
    {
        var period = await _periodRepo.GetByIdAsync(periodId)
            ?? throw new InvalidOperationException("Period not found");
        if (period.CompanyId != CompanyId)
            throw new InvalidOperationException("Period not found for this company");

        var posted = await _entryRepo.GetFilteredAsync(periodId, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var allEntries = await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id)));
        var details = allEntries.Where(e => e != null).SelectMany(e => e!.Details).ToList();

        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var leafAccounts = accounts.Where(a => a.Level >= 2 && a.IsActive).ToList();

        var items = leafAccounts.Select(a =>
        {
            var relatedDetails = details.Where(d => d.AccountId == a.Id).ToList();
            var debitMovements = relatedDetails.Sum(d => d.DebitAmount);
            var creditMovements = relatedDetails.Sum(d => d.CreditAmount);
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

    public async Task<IncomeStatementResponse> GetIncomeStatementAsync(Guid periodId)
    {
        var trialBalance = await GetTrialBalanceAsync(periodId);
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

    public async Task<BalanceSheetResponse> GetBalanceSheetAsync(Guid periodId)
    {
        var trialBalance = await GetTrialBalanceAsync(periodId);
        var period = await _periodRepo.GetByIdAsync(periodId);

        var assets = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Asset).ToList();
        var liabilities = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Liability).ToList();
        var equity = trialBalance.Items.Where(i => i.AccountType == AccountTypes.Equity).ToList();

        // Add net income to retained earnings
        var incomeSt = await GetIncomeStatementAsync(periodId);
        var retainedEarnings = equity.FirstOrDefault(e => e.AccountCode == "3.1.02");
        if (retainedEarnings != null && incomeSt.NetIncome != 0)
        {
            equity.Remove(retainedEarnings);
            equity.Add(retainedEarnings with { EndingBalance = retainedEarnings.EndingBalance + incomeSt.NetIncome });
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

    public async Task<GeneralLedgerResponse> GetGeneralLedgerAsync(Guid accountId, DateTime? fromDate, DateTime? toDate)
    {
        var account = await _accountRepo.GetByIdAsync(accountId)
            ?? throw new InvalidOperationException("Account not found");

        var posted = await _entryRepo.GetFilteredAsync(null, null, "posted", fromDate, toDate, CompanyId, 1, int.MaxValue);
        var allEntries = await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id)));
        var relevant = allEntries
            .Where(e => e != null && e.Details.Any(d => d.AccountId == accountId))
            .OrderBy(e => e!.EntryDate)
            .ToList();

        var openingBalance = account.OpeningBalance;
        var runningBalance = openingBalance;
        var isDebitNormal = account.NormalSide == "Debit";

        var items = new List<GeneralLedgerItem>();
        foreach (var entry in relevant)
        {
            var detail = entry!.Details.First(d => d.AccountId == accountId);
            runningBalance += isDebitNormal ? detail.DebitAmount - detail.CreditAmount : detail.CreditAmount - detail.DebitAmount;

            items.Add(new GeneralLedgerItem(
                entry.EntryDate, entry.EntryNumber, entry.Description, entry.ReferenceType,
                detail.DebitAmount, detail.CreditAmount, runningBalance
            ));
        }

        return new GeneralLedgerResponse(
            account.Code, account.Name, openingBalance,
            items.Sum(i => i.DebitAmount), items.Sum(i => i.CreditAmount), runningBalance,
            items
        );
    }
}
