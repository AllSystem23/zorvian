using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly ZorvianDbContext _db;
    public AccountRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<Account>> GetAllAsync(Guid companyId) =>
        await _db.Set<Account>().Where(a => a.CompanyId == companyId).OrderBy(a => a.Code).ToListAsync();

    public async Task<Account?> GetByIdAsync(Guid id) =>
        await _db.Set<Account>().Include(a => a.Children).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<Account>> GetByTypeAsync(string type, Guid companyId) =>
        await _db.Set<Account>().Where(a => a.Type == type && a.CompanyId == companyId).OrderBy(a => a.Code).ToListAsync();

    public async Task<List<Account>> GetByParentAsync(Guid parentId) =>
        await _db.Set<Account>().Where(a => a.ParentId == parentId).OrderBy(a => a.Code).ToListAsync();

    public async Task<Account?> GetByCodeAsync(string code, Guid companyId) =>
        await _db.Set<Account>().FirstOrDefaultAsync(a => a.Code == code && a.CompanyId == companyId);

    public async Task<List<Account>> GetByCodesAsync(string[] codes, Guid companyId) =>
        await _db.Set<Account>().Where(a => codes.Contains(a.Code) && a.CompanyId == companyId).ToListAsync();

    public async Task<List<Account>> GetActiveAsync(Guid companyId) =>
        await _db.Set<Account>().Where(a => a.IsActive && a.CompanyId == companyId).OrderBy(a => a.Code).ToListAsync();

    public async Task<bool> CodeExistsAsync(string code, Guid companyId) =>
        await _db.Set<Account>().AnyAsync(a => a.Code == code && a.CompanyId == companyId);

    public async Task<int> GetMaxLevelAsync(Guid? parentId, Guid companyId) =>
        await _db.Set<Account>().Where(a => a.ParentId == parentId && a.CompanyId == companyId).MaxAsync(a => (int?)a.Level) ?? 0;

    public async Task<bool> HasChildrenAsync(Guid id) =>
        await _db.Set<Account>().AnyAsync(a => a.ParentId == id);

    public async Task AddAsync(Account account) => await _db.Set<Account>().AddAsync(account);
    public Task UpdateAsync(Account account) { _db.Set<Account>().Update(account); return Task.CompletedTask; }
    public Task DeleteAsync(Account account) { _db.Set<Account>().Remove(account); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class AccountingEntryRepository : IAccountingEntryRepository
{
    private readonly ZorvianDbContext _db;
    public AccountingEntryRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<AccountingEntry>> GetListByIdsAsync(IEnumerable<Guid> ids) =>
        await _db.Set<AccountingEntry>()
            .Include(e => e.AccountingPeriod)
            .Include(e => e.Details).ThenInclude(d => d.Account)
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

    public async Task<AccountingEntry?> GetByIdAsync(Guid id) =>
        await _db.Set<AccountingEntry>()
            .Include(e => e.AccountingPeriod)
            .Include(e => e.Details).ThenInclude(d => d.Account)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<AccountingEntry>> GetPostedWithDetailsAsync(Guid? periodId, Guid companyId, DateTime? toDate = null)
    {
        var query = _db.Set<AccountingEntry>()
            .Include(e => e.Details).ThenInclude(d => d.Account)
            .Where(e => e.CompanyId == companyId && e.Status == "posted")
            .AsQueryable();

        if (periodId.HasValue)
            query = query.Where(e => e.AccountingPeriodId == periodId.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.EntryDate < toDate.Value);

        return await query.ToListAsync();
    }

    public async Task<List<AccountingEntry>> GetFilteredAsync(Guid? periodId, string? referenceType, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize)
    {
        var query = _db.Set<AccountingEntry>()
            .Include(e => e.AccountingPeriod)
            .Where(e => e.CompanyId == companyId).AsQueryable();
        if (periodId.HasValue) query = query.Where(e => e.AccountingPeriodId == periodId.Value);
        if (!string.IsNullOrWhiteSpace(referenceType)) query = query.Where(e => e.ReferenceType == referenceType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        if (fromDate.HasValue) query = query.Where(e => e.EntryDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.EntryDate <= toDate.Value);
        return await query.OrderByDescending(e => e.EntryDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> GetFilteredCountAsync(Guid? periodId, string? referenceType, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId)
    {
        var query = _db.Set<AccountingEntry>().Where(e => e.CompanyId == companyId).AsQueryable();
        if (periodId.HasValue) query = query.Where(e => e.AccountingPeriodId == periodId.Value);
        if (!string.IsNullOrWhiteSpace(referenceType)) query = query.Where(e => e.ReferenceType == referenceType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(e => e.Status == status);
        if (fromDate.HasValue) query = query.Where(e => e.EntryDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.EntryDate <= toDate.Value);
        return await query.CountAsync();
    }

    public async Task<string> GenerateEntryNumberAsync(Guid companyId)
    {
        if (_db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            var count = await _db.Set<AccountingEntry>().CountAsync(e => e.CompanyId == companyId);
            return $"AS-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
        }

        // Use PostgreSQL sequence for atomic, thread-safe number generation
        var raw = await _db.Database.SqlQueryRaw<int>("SELECT nextval('seq_entry_number')::int").FirstOrDefaultAsync();
        return $"AS-{DateTime.UtcNow:yyyyMMdd}-{raw:D4}";
    }

    public async Task<bool> HasEntriesForAccountAsync(Guid accountId) =>
        await _db.Set<AccountingEntryDetail>().AnyAsync(d => d.AccountId == accountId);

    public async Task AddAsync(AccountingEntry entry) => await _db.Set<AccountingEntry>().AddAsync(entry);
    public Task UpdateAsync(AccountingEntry entry) { _db.Set<AccountingEntry>().Update(entry); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class FiscalYearRepository : IFiscalYearRepository
{
    private readonly ZorvianDbContext _db;
    public FiscalYearRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<FiscalYear>> GetAllAsync(Guid companyId) =>
        await _db.Set<FiscalYear>().Where(f => f.CompanyId == companyId).OrderByDescending(f => f.Year).ToListAsync();

    public async Task<FiscalYear?> GetByIdAsync(Guid id) =>
        await _db.Set<FiscalYear>().Include(f => f.Periods).FirstOrDefaultAsync(f => f.Id == id);

    public async Task<FiscalYear?> GetCurrentOpenAsync(Guid companyId) =>
        await _db.Set<FiscalYear>().Include(f => f.Periods)
            .FirstOrDefaultAsync(f => f.Year == DateTime.UtcNow.Year && f.Status == FiscalYearStatus.Open && f.CompanyId == companyId);

    public async Task<FiscalYear?> GetByYearAsync(int year, Guid companyId) =>
        await _db.Set<FiscalYear>().Include(f => f.Periods).FirstOrDefaultAsync(f => f.Year == year && f.CompanyId == companyId);

    public async Task<FiscalYear?> GetByPeriodIdAsync(Guid periodId)
    {
        var period = await _db.Set<AccountingPeriod>().Include(p => p.FiscalYear).FirstOrDefaultAsync(p => p.Id == periodId);
        return period?.FiscalYear;
    }

    public async Task AddAsync(FiscalYear fiscalYear) => await _db.Set<FiscalYear>().AddAsync(fiscalYear);
    public Task UpdateAsync(FiscalYear fiscalYear) { _db.Set<FiscalYear>().Update(fiscalYear); return Task.CompletedTask; }
    public Task DeleteAsync(FiscalYear fiscalYear) { _db.Set<FiscalYear>().Remove(fiscalYear); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class AccountingPeriodRepository : IAccountingPeriodRepository
{
    private readonly ZorvianDbContext _db;
    public AccountingPeriodRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<AccountingPeriod>> GetListByIdsAsync(IEnumerable<Guid> ids) =>
        await _db.Set<AccountingPeriod>()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();

    public async Task<List<AccountingPeriod>> GetAllAsync(Guid companyId) =>
        await _db.Set<AccountingPeriod>().Where(p => p.CompanyId == companyId).OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToListAsync();

    public async Task<AccountingPeriod?> GetByIdAsync(Guid id) =>
        await _db.Set<AccountingPeriod>().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<AccountingPeriod?> GetCurrentOpenAsync(Guid companyId)
    {
        var now = DateTime.UtcNow;
        return await _db.Set<AccountingPeriod>().FirstOrDefaultAsync(p => p.Year == now.Year && p.Month == now.Month && p.Status == PeriodStatus.Open && p.CompanyId == companyId);
    }

    public async Task<AccountingPeriod?> GetByYearMonthAsync(int year, int month, Guid companyId) =>
        await _db.Set<AccountingPeriod>().FirstOrDefaultAsync(p => p.Year == year && p.Month == month && p.CompanyId == companyId);

    public async Task<List<AccountingPeriod>> GetByFiscalYearAsync(Guid fiscalYearId) =>
        await _db.Set<AccountingPeriod>().Where(p => p.FiscalYearId == fiscalYearId).OrderBy(p => p.Year).ThenBy(p => p.Month).ToListAsync();

    public async Task<List<AccountingPeriod>> GetOpenPeriodsAsync(Guid companyId) =>
        await _db.Set<AccountingPeriod>().Where(p => p.Status == PeriodStatus.Open && p.CompanyId == companyId).OrderBy(p => p.Year).ThenBy(p => p.Month).ToListAsync();

    public async Task<int> GetEntryCountAsync(Guid periodId) =>
        await _db.Set<AccountingEntry>().CountAsync(e => e.AccountingPeriodId == periodId);

    public async Task<decimal> GetTotalDebitAsync(Guid periodId) =>
        await _db.Set<AccountingEntry>().Where(e => e.AccountingPeriodId == periodId).SumAsync(e => e.TotalDebit);

    public async Task<decimal> GetTotalCreditAsync(Guid periodId) =>
        await _db.Set<AccountingEntry>().Where(e => e.AccountingPeriodId == periodId).SumAsync(e => e.TotalCredit);

    public async Task<bool> HasUnpostedEntriesAsync(Guid periodId) =>
        await _db.Set<AccountingEntry>().AnyAsync(e => e.AccountingPeriodId == periodId && e.Status != "posted");

    public async Task<bool> HasUnaccountedSalesAsync(Guid periodId, Guid companyId)
    {
        var period = await GetByIdAsync(periodId);
        if (period == null) return false;
        var monthStart = new DateTime(period.Year, period.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);
        var saleIds = await _db.Set<Sale>().Where(s => s.CompanyId == companyId && s.SaleDate >= monthStart && s.SaleDate <= monthEnd).Select(s => s.Id).ToListAsync();
        if (saleIds.Count == 0) return false;
        var accountedSaleIds = await _db.Set<AccountingEntry>()
            .Where(e => e.CompanyId == companyId && e.ReferenceType == "Sale" && saleIds.Contains(e.ReferenceId ?? Guid.Empty))
            .Select(e => e.ReferenceId)
            .ToListAsync();
        return saleIds.Any(s => !accountedSaleIds.Contains(s));
    }

    public async Task<bool> HasUnaccountedCreditNotesAsync(Guid periodId, Guid companyId)
    {
        var period = await GetByIdAsync(periodId);
        if (period == null) return false;
        var monthStart = new DateTime(period.Year, period.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);
        var cnIds = await _db.Set<CreditNote>().Where(cn => cn.CompanyId == companyId && cn.IssueDate >= monthStart && cn.IssueDate <= monthEnd).Select(cn => cn.Id).ToListAsync();
        if (cnIds.Count == 0) return false;
        var accountedCnIds = await _db.Set<AccountingEntry>()
            .Where(e => e.CompanyId == companyId && e.ReferenceType == "CreditNote" && cnIds.Contains(e.ReferenceId ?? Guid.Empty))
            .Select(e => e.ReferenceId)
            .ToListAsync();
        return cnIds.Any(cn => !accountedCnIds.Contains(cn));
    }

    public async Task<bool> HasPendingPayrollAsync(Guid periodId, Guid companyId)
    {
        var period = await GetByIdAsync(periodId);
        if (period == null) return false;
        var monthStart = new DateTime(period.Year, period.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);
        var payrollRunIds = await _db.Set<PayrollRun>()
            .Where(pr => pr.CompanyId == companyId && pr.CreatedAt >= monthStart && pr.CreatedAt <= monthEnd)
            .Select(pr => pr.Id)
            .ToListAsync();
        if (payrollRunIds.Count == 0) return false;
        var accountedIds = await _db.Set<AccountingEntry>()
            .Where(e => e.CompanyId == companyId && e.ReferenceType == "Payroll" && payrollRunIds.Contains(e.ReferenceId ?? Guid.Empty))
            .Select(e => e.ReferenceId)
            .ToListAsync();
        return payrollRunIds.Any(id => !accountedIds.Contains(id));
    }

    public async Task<bool> HasUnaccountedCashMovementsAsync(Guid periodId, Guid companyId)
    {
        var period = await GetByIdAsync(periodId);
        if (period == null) return false;
        var monthStart = new DateTime(period.Year, period.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);
        var movementIds = await _db.Set<CashMovement>()
            .Where(cm => cm.CompanyId == companyId && cm.CreatedAt >= monthStart && cm.CreatedAt <= monthEnd && cm.ApprovalStatus == "approved")
            .Select(cm => cm.Id)
            .ToListAsync();
        if (movementIds.Count == 0) return false;
        var accountedIds = await _db.Set<AccountingEntry>()
            .Where(e => e.CompanyId == companyId && e.ReferenceType == "CashMovement" && movementIds.Contains(e.ReferenceId ?? Guid.Empty))
            .Select(e => e.ReferenceId)
            .ToListAsync();
        return movementIds.Any(id => !accountedIds.Contains(id));
    }

    public async Task<bool> HasUnaccountedInventoryMovementsAsync(Guid periodId, Guid companyId)
    {
        var period = await GetByIdAsync(periodId);
        if (period == null) return false;
        var monthStart = new DateTime(period.Year, period.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1).AddDays(1).AddTicks(-1);
        var movementIds = await _db.Set<InventoryMovement>()
            .Where(im => im.CompanyId == companyId && im.CreatedAt >= monthStart && im.CreatedAt <= monthEnd)
            .Select(im => im.Id)
            .ToListAsync();
        if (movementIds.Count == 0) return false;
        var accountedIds = await _db.Set<AccountingEntry>()
            .Where(e => e.CompanyId == companyId && e.ReferenceType == "InventoryMovement" && movementIds.Contains(e.ReferenceId ?? Guid.Empty))
            .Select(e => e.ReferenceId)
            .ToListAsync();
        return movementIds.Any(id => !accountedIds.Contains(id));
    }

    public async Task AddAsync(AccountingPeriod period) => await _db.Set<AccountingPeriod>().AddAsync(period);
    public Task UpdateAsync(AccountingPeriod period) { _db.Set<AccountingPeriod>().Update(period); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class AccountLinkRepository : IAccountLinkRepository
{
    private readonly ZorvianDbContext _db;
    public AccountLinkRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<AccountLink>> GetByCompanyAsync(Guid companyId) =>
        await _db.Set<AccountLink>().Include(l => l.Account).Where(l => l.CompanyId == companyId).ToListAsync();

    public async Task<AccountLink?> GetByTransactionTypeAndRoleAsync(string transactionType, string role, Guid companyId) =>
        await _db.Set<AccountLink>().Include(l => l.Account).FirstOrDefaultAsync(l => l.TransactionType == transactionType && l.Role == role && l.CompanyId == companyId);

    public async Task<List<AccountLink>> GetByTransactionTypeAsync(string transactionType, Guid companyId) =>
        await _db.Set<AccountLink>().Include(l => l.Account).Where(l => l.TransactionType == transactionType && l.CompanyId == companyId).ToListAsync();

    public async Task AddAsync(AccountLink link) => await _db.Set<AccountLink>().AddAsync(link);
    public Task UpdateAsync(AccountLink link) { _db.Set<AccountLink>().Update(link); return Task.CompletedTask; }
    public Task DeleteAsync(AccountLink link) { _db.Set<AccountLink>().Remove(link); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}

public sealed class AccountingRuleRepository : IAccountingRuleRepository
{
    private readonly ZorvianDbContext _db;
    public AccountingRuleRepository(ZorvianDbContext db) => _db = db;

    public async Task<List<AccountingRule>> GetByCompanyAsync(Guid companyId) =>
        await _db.Set<AccountingRule>().Where(r => r.CompanyId == companyId).OrderBy(r => r.SortOrder).ToListAsync();

    public async Task<List<AccountingRule>> GetByEventTypeAsync(string eventType, Guid companyId) =>
        await _db.Set<AccountingRule>().Where(r => r.EventType == eventType && r.IsActive && r.CompanyId == companyId).OrderBy(r => r.SortOrder).ToListAsync();

    public async Task AddAsync(AccountingRule rule) => await _db.Set<AccountingRule>().AddAsync(rule);
    public Task UpdateAsync(AccountingRule rule) { _db.Set<AccountingRule>().Update(rule); return Task.CompletedTask; }
    public Task DeleteAsync(AccountingRule rule) { _db.Set<AccountingRule>().Remove(rule); return Task.CompletedTask; }
    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}
