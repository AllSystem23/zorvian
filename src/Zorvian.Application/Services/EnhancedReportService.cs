using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class EnhancedReportService
{
    private readonly FinancialReportService _reports;
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ITenantContext _tenant;
    private readonly ICompanyRepository _companyRepo;
    private string? _companyCurrencyCache;

    public EnhancedReportService(
        FinancialReportService reports,
        IAccountingEntryRepository entryRepo,
        IAccountingPeriodRepository periodRepo,
        IAccountRepository accountRepo,
        ITenantContext tenant,
        ICompanyRepository companyRepo)
    {
        _reports = reports; _entryRepo = entryRepo;
        _periodRepo = periodRepo; _accountRepo = accountRepo;
        _tenant = tenant; _companyRepo = companyRepo;
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

    public async Task<EquityChangeResponse> GetEquityChangesAsync(Guid periodId)
    {
        var period = await _periodRepo.GetByIdAsync(periodId)
            ?? throw new InvalidOperationException("Period not found");
        var cc = await GetCompanyCurrencyAsync();
        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var equityAccounts = accounts.Where(a => a.Type == AccountTypes.Equity && a.IsActive).OrderBy(a => a.Code).ToList();

        var prevPeriod = await _periodRepo.GetByYearMonthAsync(
            period.Year == 1 && period.Month == 1 ? period.Year - 1 : period.Year,
            period.Month == 1 ? 12 : period.Month - 1, CompanyId);
        var prevPeriods = (await Task.WhenAll(
            (prevPeriod != null ? new[] { prevPeriod } : Array.Empty<AccountingPeriod>())
            .Concat(await GetYearPeriodsAsync(period.Year, period.Month, CompanyId))
            .Select(async p => await _entryRepo.GetFilteredAsync(p.Id, null, "posted", null, null, CompanyId, 1, int.MaxValue))
        )).SelectMany(e => e).ToList();

        var currentPosted = await _entryRepo.GetFilteredAsync(periodId, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var currentEntries = (await Task.WhenAll(currentPosted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>().ToList();
        var entryMap = currentEntries.ToDictionary(e => e.Id);
        var details = currentEntries.SelectMany(e => e.Details).ToList();

        var items = new List<EquityChangeItem>();
        foreach (var acc in equityAccounts)
        {
            var relatedDetails = details.Where(d => d.AccountId == acc.Id).ToList();
            var debit = Convert(relatedDetails.Sum(d => d.DebitAmount), currentEntries.First(), cc);
            var credit = Convert(relatedDetails.Sum(d => d.CreditAmount), currentEntries.First(), cc);
            var movement = acc.NormalSide == "Credit" ? credit - debit : debit - credit;
            var additions = movement > 0 ? movement : 0;
            var deductions = movement < 0 ? -movement : 0;
            var opening = acc.OpeningBalance;
            var ending = acc.NormalSide == "Credit"
                ? opening + credit - debit
                : opening + debit - credit;

            items.Add(new EquityChangeItem(acc.Name, opening, additions, deductions, ending));
        }

        return new EquityChangeResponse(
            period.Name, DateTime.UtcNow,
            items.Sum(i => i.OpeningBalance),
            items.Sum(i => i.Additions),
            items.Sum(i => i.Deductions),
            items.Sum(i => i.EndingBalance),
            items
        );
    }

    private async Task<IEnumerable<AccountingPeriod>> GetYearPeriodsAsync(int year, int month, Guid companyId)
    {
        var all = await _periodRepo.GetAllAsync(companyId);
        return all.Where(p => p.Year == year && p.Month <= month && p.Status == "closed").OrderBy(p => p.Month);
    }

    public async Task<CashFlowStatementResponse> GetCashFlowStatementAsync(Guid periodId)
    {
        var period = await _periodRepo.GetByIdAsync(periodId)
            ?? throw new InvalidOperationException("Period not found");
        var cc = await GetCompanyCurrencyAsync();
        var posted = await _entryRepo.GetFilteredAsync(periodId, null, "posted", null, null, CompanyId, 1, int.MaxValue);
        var entries = (await Task.WhenAll(posted.Select(async e => await _entryRepo.GetByIdAsync(e.Id))))
            .Where(e => e != null).Cast<AccountingEntry>().ToList();
        var entryMap = entries.ToDictionary(e => e.Id);
        var details = entries.SelectMany(e => e.Details).ToList();
        var accounts = await _accountRepo.GetAllAsync(CompanyId);
        var accById = accounts.ToDictionary(a => a.Id);

        var cashAccounts = accounts.Where(a => a.Code.StartsWith("1.1.0") && a.IsActive).Select(a => a.Id).ToHashSet();
        var arAccounts = accounts.Where(a => a.Code.StartsWith("1.1.1") && a.IsActive).Select(a => a.Id).ToHashSet();
        var inventoryAccounts = accounts.Where(a => a.Code.StartsWith("1.1.2") && a.IsActive).Select(a => a.Id).ToHashSet();
        var apAccounts = accounts.Where(a => a.Code.StartsWith("2.1.0") && a.IsActive).Select(a => a.Id).ToHashSet();
        var liabilityAccounts = accounts.Where(a => a.Type == AccountTypes.Liability && a.IsActive).Select(a => a.Id).ToHashSet();
        var equityAccs = accounts.Where(a => a.Type == AccountTypes.Equity && a.IsActive).Select(a => a.Id).ToHashSet();
        var incomeAccounts = accounts.Where(a => a.Type == AccountTypes.Income && a.IsActive).Select(a => a.Id).ToHashSet();
        var expenseAccounts = accounts.Where(a => a.Type is AccountTypes.Expense or AccountTypes.Cost && a.IsActive).Select(a => a.Id).ToHashSet();

        var operating = new List<CashFlowStatementItem>();
        var investing = new List<CashFlowStatementItem>();
        var financing = new List<CashFlowStatementItem>();

        foreach (var entry in entries)
        {
            foreach (var detail in entry.Details)
            {
                var amount = Convert(detail.DebitAmount - detail.CreditAmount, entry, cc);
                if (amount == 0) continue;
                var absAmount = Math.Abs(amount);

                if (incomeAccounts.Contains(detail.AccountId))
                {
                    operating.Add(new CashFlowStatementItem($"Ingreso: {detail.Description ?? entry.Description}", amount, "income"));
                }
                else if (expenseAccounts.Contains(detail.AccountId))
                {
                    operating.Add(new CashFlowStatementItem($"Gasto: {detail.Description ?? entry.Description}", -absAmount, "expense"));
                }
                else if (arAccounts.Contains(detail.AccountId))
                {
                    if (amount > 0) operating.Add(new CashFlowStatementItem($"↑ Cuentas por Cobrar", -absAmount, "ar_increase"));
                    else operating.Add(new CashFlowStatementItem($"↓ Cuentas por Cobrar", absAmount, "ar_decrease"));
                }
                else if (inventoryAccounts.Contains(detail.AccountId))
                {
                    if (amount > 0) operating.Add(new CashFlowStatementItem($"↑ Inventario", -absAmount, "inv_increase"));
                    else operating.Add(new CashFlowStatementItem($"↓ Inventario", absAmount, "inv_decrease"));
                }
                else if (apAccounts.Contains(detail.AccountId))
                {
                    if (amount > 0) operating.Add(new CashFlowStatementItem($"↑ Cuentas por Pagar", absAmount, "ap_increase"));
                    else operating.Add(new CashFlowStatementItem($"↓ Cuentas por Pagar", -absAmount, "ap_decrease"));
                }
                else if (liabilityAccounts.Contains(detail.AccountId))
                {
                    if (amount > 0) financing.Add(new CashFlowStatementItem($"↑ Pasivos", absAmount, "financing_inflow"));
                    else financing.Add(new CashFlowStatementItem($"↓ Pasivos", -absAmount, "financing_outflow"));
                }
                else if (equityAccs.Contains(detail.AccountId))
                {
                    if (amount > 0) financing.Add(new CashFlowStatementItem($"↑ Patrimonio", absAmount, "equity_inflow"));
                    else financing.Add(new CashFlowStatementItem($"↓ Patrimonio", -absAmount, "equity_outflow"));
                }
                else if (detail.Description?.Contains("activo fijo", StringComparison.OrdinalIgnoreCase) == true
                    || detail.Description?.Contains("equipo", StringComparison.OrdinalIgnoreCase) == true
                    || detail.Description?.Contains("propiedad", StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (amount > 0) investing.Add(new CashFlowStatementItem(detail.Description ?? entry.Description, -absAmount, "capex"));
                    else investing.Add(new CashFlowStatementItem(detail.Description ?? entry.Description, absAmount, "disposal"));
                }
            }
        }

        var netOperating = operating.Sum(i => i.Amount);
        var netInvesting = investing.Sum(i => i.Amount);
        var netFinancing = financing.Sum(i => i.Amount);
        var netIncrease = netOperating + netInvesting + netFinancing;

        var prevPeriod = await _periodRepo.GetByYearMonthAsync(
            period.Year == 1 && period.Month == 1 ? period.Year - 1 : period.Year,
            period.Month == 1 ? 12 : period.Month - 1, CompanyId);
        var beginningCash = 0m;
        if (prevPeriod != null)
        {
            var prevTrial = await _reports.GetTrialBalanceAsync(prevPeriod.Id);
            var cashCodes = new[] { "1.1.01", "1.1.02", "1.1.03", "1.1.04", "1.1.05" };
            beginningCash = prevTrial.Items
                .Where(i => cashCodes.Any(c => i.AccountCode.StartsWith(c)))
                .Sum(i => i.EndingBalance);
        }

        return new CashFlowStatementResponse(
            period.Name, DateTime.UtcNow,
            operating, netOperating,
            investing, netInvesting,
            financing, netFinancing,
            netIncrease, beginningCash, beginningCash + netIncrease
        );
    }

    public async Task<ComparativeReportResponse> GetComparativeReportAsync(string reportType, List<Guid> periodIds)
    {
        if (periodIds.Count < 2)
            throw new InvalidOperationException("At least two periods required for comparison");

        var periodResults = await Task.WhenAll(periodIds.Select(async id => await _periodRepo.GetByIdAsync(id)));
        var periods = periodResults.Where(p => p is not null).Select(p => p!).ToArray();

        var trialBalances = await Task.WhenAll(periodIds.Select(async id => await _reports.GetTrialBalanceAsync(id)));

        var lineMap = new Dictionary<string, (string Name, string Type, List<decimal> Amounts, List<decimal> PctOfTotal)>();
        foreach (var (tb, period) in trialBalances.Zip(periods, (tb, p) => (tb, p)))
        {
            var total = tb.Items.Sum(i => i.EndingBalance);
            foreach (var item in tb.Items)
            {
                if (!lineMap.ContainsKey(item.AccountCode))
                    lineMap[item.AccountCode] = (item.AccountName, item.AccountType, new List<decimal>(), new List<decimal>());
                var entry = lineMap[item.AccountCode];
                entry.Amounts.Add(item.EndingBalance);
                entry.PctOfTotal.Add(total != 0 ? Math.Round(item.EndingBalance / total * 100, 2) : 0);
            }
        }

        var lines = lineMap.Select(kv =>
        {
            var (code, (name, type, amounts, _)) = kv;
            var periodsData = periods.Select((p, i) => new ComparativePeriod(
                p.Name,
                i < amounts.Count ? amounts[i] : 0,
                0
            )).ToList();
            var current = amounts.Count > 0 ? amounts[^1] : 0;
            var previous = amounts.Count > 1 ? amounts[0] : 0;
            var variance = current - previous;
            var variancePct = previous != 0 ? Math.Round(variance / previous * 100, 2) : 0;
            return new ComparativeLine($"{code} - {name}", type, periodsData, variance, variancePct);
        }).ToList();

        var totalCurrent = trialBalances[^1].Items.Sum(i => i.EndingBalance);
        var totalPrevious = trialBalances[0].Items.Sum(i => i.EndingBalance);
        var totalVariance = totalCurrent - totalPrevious;
        var totalVariancePct = totalPrevious != 0 ? Math.Round(totalVariance / totalPrevious * 100, 2) : 0;

        return new ComparativeReportResponse(
            reportType, DateTime.UtcNow,
            lines, totalCurrent, totalPrevious, totalVariance, totalVariancePct
        );
    }
}
