using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IAccountRepository
{
    Task<List<Account>> GetAllAsync(Guid companyId);
    Task<Account?> GetByIdAsync(Guid id);
    Task<List<Account>> GetByTypeAsync(string type, Guid companyId);
    Task<List<Account>> GetByParentAsync(Guid parentId);
    Task<Account?> GetByCodeAsync(string code, Guid companyId);
    Task<List<Account>> GetByCodesAsync(string[] codes, Guid companyId);
    Task<List<Account>> GetActiveAsync(Guid companyId);
    Task<bool> CodeExistsAsync(string code, Guid companyId);
    Task<int> GetMaxLevelAsync(Guid? parentId, Guid companyId);
    Task<bool> HasChildrenAsync(Guid id);
    Task AddAsync(Account account);
    Task UpdateAsync(Account account);
    Task DeleteAsync(Account account);
    Task SaveChangesAsync();
}

public interface IAccountingEntryRepository
{
    Task<List<AccountingEntry>> GetListByIdsAsync(IEnumerable<Guid> ids);
    Task<AccountingEntry?> GetByIdAsync(Guid id);
    Task<List<AccountingEntry>> GetPostedWithDetailsAsync(Guid? periodId, Guid companyId, DateTime? toDate = null);
    Task<List<AccountingEntry>> GetFilteredAsync(Guid? periodId, string? referenceType, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId, int page, int pageSize);
    Task<int> GetFilteredCountAsync(Guid? periodId, string? referenceType, string? status, DateTime? fromDate, DateTime? toDate, Guid companyId);
    Task<string> GenerateEntryNumberAsync(Guid companyId);
    Task<bool> HasEntriesForAccountAsync(Guid accountId);
    Task AddAsync(AccountingEntry entry);
    Task UpdateAsync(AccountingEntry entry);
    Task SaveChangesAsync();
}

public interface IAccountingPeriodRepository
{
    Task<List<AccountingPeriod>> GetListByIdsAsync(IEnumerable<Guid> ids);
    Task<List<AccountingPeriod>> GetAllAsync(Guid companyId);
    Task<AccountingPeriod?> GetByIdAsync(Guid id);
    Task<AccountingPeriod?> GetCurrentOpenAsync(Guid companyId);
    Task<AccountingPeriod?> GetByYearMonthAsync(int year, int month, Guid companyId);
    Task<List<AccountingPeriod>> GetByFiscalYearAsync(Guid fiscalYearId);
    Task<List<AccountingPeriod>> GetOpenPeriodsAsync(Guid companyId);
    Task<int> GetEntryCountAsync(Guid periodId);
    Task<decimal> GetTotalDebitAsync(Guid periodId);
    Task<decimal> GetTotalCreditAsync(Guid periodId);
    Task<bool> HasUnpostedEntriesAsync(Guid periodId);
    Task<bool> HasUnaccountedSalesAsync(Guid periodId, Guid companyId);
    Task<bool> HasUnaccountedCreditNotesAsync(Guid periodId, Guid companyId);
    Task<bool> HasPendingPayrollAsync(Guid periodId, Guid companyId);
    Task<bool> HasUnaccountedCashMovementsAsync(Guid periodId, Guid companyId);
    Task<bool> HasUnaccountedInventoryMovementsAsync(Guid periodId, Guid companyId);
    Task AddAsync(AccountingPeriod period);
    Task UpdateAsync(AccountingPeriod period);
    Task SaveChangesAsync();
}

public interface IAccountLinkRepository
{
    Task<List<AccountLink>> GetByCompanyAsync(Guid companyId);
    Task<AccountLink?> GetByTransactionTypeAndRoleAsync(string transactionType, string role, Guid companyId);
    Task<List<AccountLink>> GetByTransactionTypeAsync(string transactionType, Guid companyId);
    Task AddAsync(AccountLink link);
    Task UpdateAsync(AccountLink link);
    Task DeleteAsync(AccountLink link);
    Task SaveChangesAsync();
}

public interface IAccountingRuleRepository
{
    Task<List<AccountingRule>> GetByCompanyAsync(Guid companyId);
    Task<List<AccountingRule>> GetByEventTypeAsync(string eventType, Guid companyId);
    Task AddAsync(AccountingRule rule);
    Task UpdateAsync(AccountingRule rule);
    Task DeleteAsync(AccountingRule rule);
    Task SaveChangesAsync();
}
