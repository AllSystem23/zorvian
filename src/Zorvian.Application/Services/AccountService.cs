using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.Helpers;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class AccountService
{
    private readonly IAccountRepository _repo;
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;
    private readonly ICompanyRepository _companyRepo;
    private Guid? _cachedCompanyId;

    public AccountService(IAccountRepository repo, IAccountingEntryRepository entryRepo, ITenantContext tenant, IMapper mapper, ICompanyRepository companyRepo)
    {
        _repo = repo; _entryRepo = entryRepo; _tenant = tenant; _mapper = mapper; _companyRepo = companyRepo;
    }

    private async Task<Guid> GetCompanyIdAsync()
    {
        if (_cachedCompanyId.HasValue) return _cachedCompanyId.Value;

        if (_tenant.TenantId.TryGetCompanyId(out var id) && id != Guid.Empty)
        {
            _cachedCompanyId = id;
            return id;
        }

        // For SuperAdmin, try to resolve the first company from the DB
        if (_tenant.IsSuperAdmin)
        {
            var firstCompany = await _companyRepo.GetFirstActiveAsync();
            if (firstCompany is not null)
            {
                _cachedCompanyId = firstCompany.Id;
                return firstCompany.Id;
            }
            return Guid.Empty;
        }

        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId ?? "");
        if (company is not null)
        {
            _cachedCompanyId = company.Id;
            return company.Id;
        }

        throw new InvalidOperationException("Tenant not configured. Switch to a company first.");
    }

    public async Task<List<AccountResponse>> GetAllAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var accounts = await _repo.GetAllAsync(companyId);
        return accounts.Select(a => MapWithBalance(a)).ToList();
    }

    public async Task<List<AccountResponse>> GetTreeAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var all = await _repo.GetAllAsync(companyId);
        var roots = all.Where(a => a.ParentId == null).OrderBy(a => a.Code).ToList();
        return roots.Select(r => BuildTree(r, all)).ToList();
    }

    private AccountResponse BuildTree(Account account, List<Account> all)
    {
        var children = all.Where(a => a.ParentId == account.Id).OrderBy(a => a.Code).ToList();
        return MapWithBalance(account) with
        {
            Children = children.Select(c => BuildTree(c, all)).ToList()
        };
    }

    private AccountResponse MapWithBalance(Account a)
    {
        return new AccountResponse(
            a.Id, a.Code, a.Name, a.Description, a.Type, a.NormalSide,
            a.ParentId, null, a.Level, a.IsActive, a.OpeningBalance, 0,
            a.Children?.Select(c => MapWithBalance(c)).ToList() ?? []
        );
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        var companyId = await GetCompanyIdAsync();
        if (await _repo.CodeExistsAsync(request.Code, companyId))
            throw new InvalidOperationException($"Account code '{request.Code}' already exists");

        var account = _mapper.Map<Account>(request);
        if (request.ParentId.HasValue)
        {
            var parent = await _repo.GetByIdAsync(request.ParentId.Value)
                ?? throw new InvalidOperationException("Parent account not found");
            account.Level = parent.Level + 1;
        }

        await _repo.AddAsync(account);
        await _repo.SaveChangesAsync();
        return MapWithBalance(account);
    }

    public async Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request)
    {
        var account = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Account not found");
        var companyId = await GetCompanyIdAsync();
        if (request.Code != null)
        {
            if (await _repo.CodeExistsAsync(request.Code, companyId) && request.Code != account.Code)
                throw new InvalidOperationException($"Account code '{request.Code}' already exists");
            account.Code = request.Code;
        }
        if (request.Name != null) account.Name = request.Name;
        if (request.Description != null) account.Description = request.Description;
        if (request.Type != null) account.Type = request.Type;
        if (request.NormalSide != null) account.NormalSide = request.NormalSide;
        if (request.IsActive.HasValue) account.IsActive = request.IsActive.Value;
        if (request.OpeningBalance.HasValue) account.OpeningBalance = request.OpeningBalance.Value;
        if (request.Level.HasValue) account.Level = request.Level.Value;
        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
                throw new InvalidOperationException("Account cannot be its own parent");
            var parent = await _repo.GetByIdAsync(request.ParentId.Value)
                ?? throw new InvalidOperationException("Parent account not found");
            account.ParentId = request.ParentId;
            account.Level = parent.Level + 1;
        }
        await _repo.UpdateAsync(account);
        await _repo.SaveChangesAsync();
        return MapWithBalance(account);
    }

    public async Task DeleteAsync(Guid id)
    {
        var account = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Account not found");
        if (account.IsSystem)
            throw new InvalidOperationException("Cannot delete a system account");
        bool hasEntries = await _entryRepo.HasEntriesForAccountAsync(id);
        if (hasEntries)
            throw new InvalidOperationException("Cannot delete account with associated entries");
        bool hasChildren = await _repo.HasChildrenAsync(id);
        if (hasChildren)
            throw new InvalidOperationException("Cannot delete account with child accounts");
        await _repo.DeleteAsync(account);
        await _repo.SaveChangesAsync();
    }

    public async Task ImportFromCsvAsync(string csvContent)
    {
        var companyId = await GetCompanyIdAsync();
        var lines = csvContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Skip(1);

        var tempIds = new Dictionary<string, Guid>();

        foreach (var line in lines)
        {
            var parts = line.Split(',');
            if (parts.Length < 7) continue;

            var code = parts[0].Trim();
            var name = parts[1].Trim();
            var nature = parts[2].Trim() == "D" ? AccountSide.Debit : AccountSide.Credit;
            var level = int.Parse(parts[3].Trim());
            var isAuto = bool.Parse(parts[6].Trim());

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                NormalSide = nature,
                Level = level,
                IsActive = true,
                Type = MapType(code),
                IsSystem = isAuto,
                TenantId = _tenant.TenantId ?? companyId.ToString(),
                CreatedBy = "System",
                CreatedAt = DateTime.UtcNow
            };

            tempIds[code] = account.Id;

            // Resolve ParentId
            var parentCode = GetParentCode(code);
            if (parentCode != null && tempIds.TryGetValue(parentCode, out var parentId))
            {
                account.ParentId = parentId;
            }

            await _repo.AddAsync(account);
        }

        await _repo.SaveChangesAsync();
    }

    private string MapType(string code)
    {
        if (code.StartsWith("1")) return AccountTypes.Asset;
        if (code.StartsWith("2")) return AccountTypes.Liability;
        if (code.StartsWith("3")) return AccountTypes.Equity;
        if (code.StartsWith("4")) return AccountTypes.Income;
        if (code.StartsWith("5")) return AccountTypes.Cost;
        if (code.StartsWith("6")) return AccountTypes.Expense;
        return AccountTypes.Asset;
    }

    private string? GetParentCode(string code)
    {
        var parts = code.Split('.');
        if (parts.Length <= 1) return null;

        for (int i = parts.Length - 1; i >= 0; i--)
        {
            if (parts[i] != "00" && parts[i] != "0" && parts[i] != "000" && parts[i] != "0000")
            {
                var parentParts = (string[])parts.Clone();
                parentParts[i] = new string('0', parts[i].Length);
                var potentialParent = string.Join(".", parentParts);
                if (potentialParent != code) return potentialParent;
            }
        }
        return null;
    }

    public async Task SeedDefaultChartOfAccountsAsync()
    {
        var companyId = await GetCompanyIdAsync();
        var count = (await _repo.GetAllAsync(companyId)).Count;
        if (count > 0) return;

        // Step 1: Generate explicit IDs and build code→Id map for parent resolution
        var idMap = new Dictionary<string, Guid>();
        string[] codes = ["1","1.1","1.1.01","1.1.02","1.1.03","1.1.04","1.1.05",
                          "1.2","1.2.01","1.2.02",
                          "2","2.1","2.1.01","2.1.02","2.1.03",
                          "3","3.1.01","3.1.02","3.1.03",
                          "4","4.1.01","4.4.01",
                          "5","5.1.01",
                          "6","6.1.01","6.1.02","6.1.04","6.1.05","6.2.01"];
        foreach (var code in codes) idMap[code] = Guid.NewGuid();

        // Parent code lookup: "1.1.02" → "1.1", "1.1" → "1"
        string? GetParentCode(string code)
        {
            var parts = code.Split('.');
            if (parts.Length <= 1) return null;
            return string.Join('.', parts[..^1]);
        }

        // Step 2: Create all accounts WITHOUT ParentId (avoids FK ordering issues)
        var accounts = new List<Account>
        {
            new() { Id = idMap["1"], Code = "1", Name = "Activos", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["1.1"], Code = "1.1", Name = "Activos Circulantes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["1.1.01"], Code = "1.1.01", Name = "Caja General", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.1.02"], Code = "1.1.02", Name = "Bancos", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.1.03"], Code = "1.1.03", Name = "Clientes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.1.04"], Code = "1.1.04", Name = "Inventario", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.1.05"], Code = "1.1.05", Name = "IVA Crédito Fiscal", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.2"], Code = "1.2", Name = "Activos No Circulantes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["1.2.01"], Code = "1.2.01", Name = "Propiedad, Planta y Equipo", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["1.2.02"], Code = "1.2.02", Name = "Depreciación Acumulada", Type = AccountTypes.Asset, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true },

            new() { Id = idMap["2"], Code = "2", Name = "Pasivos", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["2.1"], Code = "2.1", Name = "Pasivos Circulantes", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["2.1.01"], Code = "2.1.01", Name = "Proveedores", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["2.1.02"], Code = "2.1.02", Name = "IVA Débito Fiscal", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["2.1.03"], Code = "2.1.03", Name = "ISR por Pagar", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true },

            new() { Id = idMap["3"], Code = "3", Name = "Patrimonio", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["3.1.01"], Code = "3.1.01", Name = "Capital Social", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["3.1.02"], Code = "3.1.02", Name = "Utilidades Retenidas", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["3.1.03"], Code = "3.1.03", Name = "Superávit por Revaluación", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true },

            new() { Id = idMap["4"], Code = "4", Name = "Ingresos", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["4.1.01"], Code = "4.1.01", Name = "Ventas", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["4.4.01"], Code = "4.4.01", Name = "Ganancia en Venta de Activos", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true },

            new() { Id = idMap["5"], Code = "5", Name = "Costos", Type = AccountTypes.Cost, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["5.1.01"], Code = "5.1.01", Name = "Costo de Ventas", Type = AccountTypes.Cost, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },

            new() { Id = idMap["6"], Code = "6", Name = "Gastos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Id = idMap["6.1.01"], Code = "6.1.01", Name = "Gastos Administrativos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["6.1.02"], Code = "6.1.02", Name = "Gastos de Venta", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["6.1.04"], Code = "6.1.04", Name = "Depreciación", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["6.1.05"], Code = "6.1.05", Name = "Mantenimiento y Reparaciones", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },
            new() { Id = idMap["6.2.01"], Code = "6.2.01", Name = "Pérdida en Baja de Activos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true },
        };

        // STEP 2a: Insert all accounts (no ParentId = no FK violations)
        foreach (var a in accounts)
            await _repo.AddAsync(a);
        await _repo.SaveChangesAsync();

        // STEP 2b: Now update ParentId for each account that has a parent
        // All rows exist in the DB, so FK constraints are satisfied
        foreach (var a in accounts)
        {
            var parentCode = GetParentCode(a.Code);
            if (parentCode != null && idMap.TryGetValue(parentCode, out var parentId))
            {
                a.ParentId = parentId;
                await _repo.UpdateAsync(a);
            }
        }
        await _repo.SaveChangesAsync();
    }
}

public sealed class AccountingEntryService
{
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public AccountingEntryService(IAccountingEntryRepository entryRepo, IAccountingPeriodRepository periodRepo, IAccountRepository accountRepo, ITenantContext tenant, IMapper mapper)
    {
        _entryRepo = entryRepo; _periodRepo = periodRepo; _accountRepo = accountRepo; _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => _tenant.ResolveCompanyId();

    public async Task<AccountingEntryResponse> CreateManualEntryAsync(CreateManualEntryRequest request)
    {
        var period = await _periodRepo.GetByIdAsync(request.AccountingPeriodId)
            ?? throw new KeyNotFoundException("Period not found");
        if (period.Status != "open")
            throw new InvalidOperationException("Period is not open");

        var totalDebit = request.Details.Sum(d => d.DebitAmount);
        var totalCredit = request.Details.Sum(d => d.CreditAmount);
        if (totalDebit != totalCredit)
            throw new InvalidOperationException("Total debits must equal total credits");
        if (totalDebit == 0)
            throw new InvalidOperationException("Entry must have at least one line");

        var entry = new AccountingEntry
        {
            EntryNumber = await _entryRepo.GenerateEntryNumberAsync(CompanyId),
            EntryDate = request.EntryDate,
            Description = request.Description,
            ReferenceType = "manual",
            Status = "posted",
            AccountingPeriodId = request.AccountingPeriodId,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            PostedAt = DateTime.UtcNow,
            Details = request.Details.Select(d => new AccountingEntryDetail
            {
                AccountId = d.AccountId,
                DebitAmount = d.DebitAmount,
                CreditAmount = d.CreditAmount,
                Description = d.Description,
            }).ToList(),
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return await GetByIdAsync(entry.Id) ?? throw new InvalidOperationException("Failed to create entry");
    }

    public async Task<AccountingEntryResponse?> GetByIdAsync(Guid id)
    {
        var entry = await _entryRepo.GetByIdAsync(id);
        if (entry is null) return null;
        return MapEntry(entry);
    }

    public async Task<PagedResult<AccountingEntryListResponse>> GetFilteredAsync(
        Guid? periodId, string? referenceType, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var items = await _entryRepo.GetFilteredAsync(periodId, referenceType, status, fromDate, toDate, CompanyId, page, pageSize);
        var total = await _entryRepo.GetFilteredCountAsync(periodId, referenceType, status, fromDate, toDate, CompanyId);
        return new PagedResult<AccountingEntryListResponse>(
            items.Select(e => new AccountingEntryListResponse(
                e.Id, e.EntryNumber, e.EntryDate, e.Description, e.ReferenceType,
                e.Status, e.TotalDebit, e.TotalCredit, e.AccountingPeriod?.Name
            )).ToList(), total, page, pageSize
        );
    }

    public async Task<AccountingEntryResponse> PostEntryAsync(Guid id)
    {
        var entry = await _entryRepo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Entry not found");
        if (entry.Status == "posted") throw new InvalidOperationException("Entry already posted");
        entry.Status = "posted";
        entry.PostedAt = DateTime.UtcNow;
        await _entryRepo.UpdateAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return MapEntry(entry);
    }

    private AccountingEntryResponse MapEntry(AccountingEntry e) => new(
        e.Id, e.EntryNumber, e.EntryDate, e.Description, e.ReferenceType, e.ReferenceId,
        e.Status, e.AccountingPeriodId, e.AccountingPeriod?.Name, e.TotalDebit, e.TotalCredit,
        e.CreatedAt, e.PostedAt, e.CreatedBy,
        e.Details.Select(d => new AccountingEntryDetailItem(
            d.AccountId, d.Account?.Code, d.Account?.Name, d.DebitAmount, d.CreditAmount, d.Description
        )).ToList()
    );
}

public sealed record PeriodCloseValidation(
    bool CanClose,
    List<string> Warnings,
    List<string> Errors
);

public sealed class AccountingPeriodService
{
    private readonly IAccountingPeriodRepository _repo;
    private readonly IFiscalYearRepository _fiscalYearRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;

    public AccountingPeriodService(
        IAccountingPeriodRepository repo,
        IFiscalYearRepository fiscalYearRepo,
        ICompanyRepository companyRepo,
        ITenantContext tenant)
    {
        _repo = repo;
        _fiscalYearRepo = fiscalYearRepo;
        _companyRepo = companyRepo;
        _tenant = tenant;
    }

    private Guid CompanyId => _tenant.ResolveCompanyId();

    public async Task<List<AccountingPeriodResponse>> GetAllAsync()
    {
        var periods = await _repo.GetAllAsync(CompanyId);
        return periods.Select(p => new AccountingPeriodResponse(
            p.Id, p.Year, p.Month, p.Name, p.Status, p.OpenedAt, p.ClosedAt,
            p.FiscalYearId, p.FiscalYear?.Name, p.CloseNotes, p.ReopenedAt, p.ReopenReason
        )).ToList();
    }

    public async Task<AccountingPeriodResponse> OpenAsync(int year, int month, Guid? fiscalYearId = null)
    {
        var existing = await _repo.GetByYearMonthAsync(year, month, CompanyId);
        if (existing != null) throw new InvalidOperationException("Period already exists");

        if (fiscalYearId == null)
        {
            var fiscalYear = await _fiscalYearRepo.GetByYearAsync(year, CompanyId);
            if (fiscalYear == null)
            {
                var settings = await _companyRepo.GetSettingsAsync(CompanyId);
                var (fyStart, fyEnd, _) = FiscalYearHelper.ResolveFiscalYearDates(
                    year, settings?.FiscalYearStartMonth, null);

                fiscalYear = new FiscalYear
                {
                    Year = year,
                    Name = $"Año Fiscal {year}",
                    StartDate = fyStart,
                    EndDate = fyEnd,
                    Status = FiscalYearStatus.Open,
                    OpenedAt = DateTime.UtcNow,
                    CompanyId = CompanyId,
                };
                await _fiscalYearRepo.AddAsync(fiscalYear);
                await _fiscalYearRepo.SaveChangesAsync();
            }
            fiscalYearId = fiscalYear.Id;
        }

        var period = new AccountingPeriod
        {
            Year = year, Month = month,
            Name = $"{month:D2}-{year}",
            Status = PeriodStatus.Open,
            OpenedAt = DateTime.UtcNow,
            FiscalYearId = fiscalYearId,
            CompanyId = CompanyId,
        };
        await _repo.AddAsync(period);
        await _repo.SaveChangesAsync();
        return new AccountingPeriodResponse(period.Id, period.Year, period.Month, period.Name, period.Status, period.OpenedAt, period.ClosedAt, period.FiscalYearId);
    }

    public async Task<PeriodCloseValidation> ValidateCloseAsync(Guid id)
    {
        var period = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Period not found");
        var errors = new List<string>();
        var warnings = new List<string>();

        if (period.Status == PeriodStatus.Closed)
            errors.Add("El período ya está cerrado");

        // Validate sequential closing: previous month must be closed first
        var prevMonth = period.Month == 1 ? 12 : period.Month - 1;
        var prevYear = period.Month == 1 ? period.Year - 1 : period.Year;
        if (prevYear >= 2020) // avoid checking before company inception
        {
            var prevPeriod = await _repo.GetByYearMonthAsync(prevYear, prevMonth, CompanyId);
            if (prevPeriod != null && prevPeriod.Status == PeriodStatus.Open)
                errors.Add($"El período anterior ({prevPeriod.Name}) aún está abierto. Debe cerrarlo primero.");
        }

        var entryCount = await _repo.GetEntryCountAsync(id);
        if (entryCount == 0)
            warnings.Add("El período no tiene asientos contables");

        var hasUnposted = await _repo.HasUnpostedEntriesAsync(id);
        if (hasUnposted)
            errors.Add("Hay asientos en estado borrador sin contabilizar");

        var totalDebit = await _repo.GetTotalDebitAsync(id);
        var totalCredit = await _repo.GetTotalCreditAsync(id);
        if (totalDebit != totalCredit)
            errors.Add($"El período no está balanceado: Débitos {totalDebit:N2} vs Créditos {totalCredit:N2} (diferencia: {Math.Abs(totalDebit - totalCredit):N2})");

        var hasUnaccountedSales = await _repo.HasUnaccountedSalesAsync(id, CompanyId);
        if (hasUnaccountedSales)
            warnings.Add("Hay ventas en el período sin asiento contable");

        var hasUnaccountedCN = await _repo.HasUnaccountedCreditNotesAsync(id, CompanyId);
        if (hasUnaccountedCN)
            warnings.Add("Hay notas de crédito en el período sin asiento contable");

        var hasPendingPayroll = await _repo.HasPendingPayrollAsync(id, CompanyId);
        if (hasPendingPayroll)
            errors.Add("Hay nóminas en el período sin asiento contable");

        var hasUnaccountedCash = await _repo.HasUnaccountedCashMovementsAsync(id, CompanyId);
        if (hasUnaccountedCash)
            warnings.Add("Hay movimientos de caja aprobados sin asiento contable");

        var hasUnaccountedInventory = await _repo.HasUnaccountedInventoryMovementsAsync(id, CompanyId);
        if (hasUnaccountedInventory)
            warnings.Add("Hay movimientos de inventario en el período sin asiento contable");

        return new PeriodCloseValidation(
            CanClose: errors.Count == 0,
            Warnings: warnings,
            Errors: errors
        );
    }

    public async Task<AccountingPeriodResponse> CloseAsync(Guid id, string? notes = null)
    {
        var validation = await ValidateCloseAsync(id);
        if (!validation.CanClose)
            throw new InvalidOperationException(
                $"No se puede cerrar el período:\n{string.Join("\n", validation.Errors)}");

        var period = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Period not found");
        period.Status = PeriodStatus.Closed;
        period.ClosedAt = DateTime.UtcNow;
        period.ClosedBy = _tenant.GetUserIdentifier();
        period.CloseNotes = notes;

        await _repo.UpdateAsync(period);
        await _repo.SaveChangesAsync();

        // Check if all periods in the fiscal year are closed -> auto-close fiscal year
        if (period.FiscalYearId.HasValue)
        {
            var fyPeriods = await _repo.GetByFiscalYearAsync(period.FiscalYearId.Value);
            if (fyPeriods.All(p => p.Status == PeriodStatus.Closed))
            {
                var fiscalYear = await _fiscalYearRepo.GetByIdAsync(period.FiscalYearId.Value);
                if (fiscalYear != null && fiscalYear.Status == FiscalYearStatus.Open)
                {
                    fiscalYear.Status = FiscalYearStatus.Closed;
                    fiscalYear.ClosedAt = DateTime.UtcNow;
                    await _fiscalYearRepo.UpdateAsync(fiscalYear);
                    await _fiscalYearRepo.SaveChangesAsync();
                }
            }
        }

        return new AccountingPeriodResponse(period.Id, period.Year, period.Month, period.Name, period.Status, period.OpenedAt, period.ClosedAt,
            period.FiscalYearId, null, period.CloseNotes, period.ReopenedAt, period.ReopenReason);
    }

    public async Task<AccountingPeriodResponse> ReopenAsync(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Debe proporcionar una razón para la re-apertura");

        var period = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Period not found");
        if (period.Status != PeriodStatus.Closed)
            throw new InvalidOperationException("Solo se puede re-abrir un período cerrado");

        // Check if fiscal year is already audited -> block reopen
        if (period.FiscalYearId.HasValue)
        {
            var fiscalYear = await _fiscalYearRepo.GetByIdAsync(period.FiscalYearId.Value);
            if (fiscalYear != null && fiscalYear.Status == FiscalYearStatus.Audited)
                throw new InvalidOperationException("No se puede re-abrir un período de un año fiscal auditado");
        }

        period.Status = PeriodStatus.Open;
        period.ReopenedAt = DateTime.UtcNow;
        period.ReopenedBy = _tenant.GetUserIdentifier();
        period.ReopenReason = reason;

        // If fiscal year was auto-closed, re-open it too
        if (period.FiscalYearId.HasValue)
        {
            var fiscalYear = await _fiscalYearRepo.GetByIdAsync(period.FiscalYearId.Value);
            if (fiscalYear != null && fiscalYear.Status == FiscalYearStatus.Closed)
            {
                fiscalYear.Status = FiscalYearStatus.Open;
                fiscalYear.ClosedAt = null;
                await _fiscalYearRepo.UpdateAsync(fiscalYear);
            }
        }

        await _repo.UpdateAsync(period);
        await _repo.SaveChangesAsync();

        return new AccountingPeriodResponse(period.Id, period.Year, period.Month, period.Name, period.Status, period.OpenedAt, period.ClosedAt,
            period.FiscalYearId, null, period.CloseNotes, period.ReopenedAt, period.ReopenReason);
    }
}

public sealed class FiscalYearService
{
    private readonly IFiscalYearRepository _repo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly ICompanyRepository _companyRepo;
    private readonly ICountryTaxConfigRepository _taxConfigRepo;
    private readonly ITenantContext _tenant;

    public FiscalYearService(IFiscalYearRepository repo, IAccountingPeriodRepository periodRepo, ICompanyRepository companyRepo, ICountryTaxConfigRepository taxConfigRepo, ITenantContext tenant)
    { _repo = repo; _periodRepo = periodRepo; _companyRepo = companyRepo; _taxConfigRepo = taxConfigRepo; _tenant = tenant; }

    private Guid CompanyId => _tenant.ResolveCompanyId();

    public async Task<List<FiscalYearResponse>> GetAllAsync()
    {
        var years = await _repo.GetAllAsync(CompanyId);
        return years.Select(MapYear).ToList();
    }

    public async Task<FiscalYearResponse> OpenAsync(int year, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var existing = await _repo.GetByYearAsync(year, CompanyId);
        if (existing != null) throw new InvalidOperationException($"Fiscal year {year} already exists");

        if (startDate == null || endDate == null)
        {
            var company = await _companyRepo.GetByIdAsync(CompanyId);
            var settings = await _companyRepo.GetSettingsAsync(CompanyId);
            var countryCode = FiscalYearHelper.MapCountryToCode(company?.Country);
            var countryConfig = await _taxConfigRepo.GetByCountryCodeAsync(countryCode);
            var (resolvedStart, resolvedEnd, _) = FiscalYearHelper.ResolveFiscalYearDates(
                year, settings?.FiscalYearStartMonth, countryConfig?.DefaultFiscalStartMonth);
            startDate ??= resolvedStart;
            endDate ??= resolvedEnd;
        }

        var fiscalYear = new FiscalYear
        {
            Year = year,
            Name = $"Año Fiscal {year}",
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            Status = FiscalYearStatus.Open,
            OpenedAt = DateTime.UtcNow,
            CompanyId = CompanyId,
        };
        await _repo.AddAsync(fiscalYear);
        await _repo.SaveChangesAsync();
        return MapYear(fiscalYear);
    }

    public async Task<FiscalYearResponse> CloseAsync(Guid id)
    {
        var fiscalYear = await _repo.GetByIdAsync(id) ?? throw new KeyNotFoundException("Fiscal year not found");
        var periods = await _periodRepo.GetByFiscalYearAsync(id);

        var openPeriods = periods.Where(p => p.Status == PeriodStatus.Open).ToList();
        if (openPeriods.Any())
            throw new InvalidOperationException(
                $"Cannot close fiscal year: {openPeriods.Count} period(s) still open: {string.Join(", ", openPeriods.Select(p => p.Name))}");

        fiscalYear.Status = FiscalYearStatus.Closed;
        fiscalYear.ClosedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(fiscalYear);
        await _repo.SaveChangesAsync();
        return MapYear(fiscalYear);
    }

    /// <summary>
    /// Returns the resolved fiscal year configuration for the current company.
    /// Primary source: CompanySettings.FiscalYearStartMonth (configurable by admin).
    /// Fallback: country-based auto-detection via FiscalYearHelper.
    /// </summary>
    public async Task<object> GetFiscalYearConfigAsync(int year)
    {
        var company = await _companyRepo.GetByIdAsync(CompanyId);
        var settings = await _companyRepo.GetSettingsAsync(CompanyId);
        int? configMonth = settings?.FiscalYearStartMonth > 0 ? settings!.FiscalYearStartMonth : null;

        var (startDate, endDate, effectiveMonth) = FiscalYearHelper.ResolveFiscalYearDates(
            year, configMonth, null);

        var existingYear = await _repo.GetByYearAsync(year, CompanyId);

        return new
        {
            Year = year,
            Country = company?.Country ?? "",
            ConfiguredStartMonth = configMonth,
            EffectiveStartMonth = effectiveMonth,
            Label = FiscalYearHelper.GetFiscalYearLabel(effectiveMonth),
            StartDate = startDate,
            EndDate = endDate,
            Source = configMonth.HasValue ? "settings" : "default",
            ExistingFiscalYearId = existingYear?.Id,
        };
    }

    private static FiscalYearResponse MapYear(FiscalYear f) => new(
        f.Id, f.Year, f.Name, f.StartDate, f.EndDate, f.Status, f.OpenedAt, f.ClosedAt, f.AuditedAt, f.AuditedBy
    );
}
