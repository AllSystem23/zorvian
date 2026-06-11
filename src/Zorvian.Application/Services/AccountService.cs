using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.DTOs.Common;
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

    public AccountService(IAccountRepository repo, IAccountingEntryRepository entryRepo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo; _entryRepo = entryRepo; _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<List<AccountResponse>> GetAllAsync()
    {
        var accounts = await _repo.GetAllAsync(CompanyId);
        return accounts.Select(a => MapWithBalance(a)).ToList();
    }

    public async Task<List<AccountResponse>> GetTreeAsync()
    {
        var all = await _repo.GetAllAsync(CompanyId);
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
        if (await _repo.CodeExistsAsync(request.Code, CompanyId))
            throw new InvalidOperationException($"Account code '{request.Code}' already exists");

        var account = _mapper.Map<Account>(request);
        account.CompanyId = CompanyId;
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
        var account = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Account not found");
        if (request.Code != null)
        {
            if (await _repo.CodeExistsAsync(request.Code, CompanyId) && request.Code != account.Code)
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
        var account = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Account not found");
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
        var companyId = CompanyId;
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
                CompanyId = companyId,
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
        var companyId = CompanyId;
        var count = (await _repo.GetAllAsync(companyId)).Count;
        if (count > 0) return;

        var accounts = new List<Account>
        {
            new() { Code = "1", Name = "Activos", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "1.1", Name = "Activos Circulantes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.1.01", Name = "Caja General", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.1.02", Name = "Bancos", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.1.03", Name = "Clientes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.1.04", Name = "Inventario", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.1.05", Name = "IVA Crédito Fiscal", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.2", Name = "Activos No Circulantes", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.2.01", Name = "Propiedad, Planta y Equipo", Type = AccountTypes.Asset, NormalSide = AccountSide.Debit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "1.2.02", Name = "Depreciación Acumulada", Type = AccountTypes.Asset, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },

            new() { Code = "2", Name = "Pasivos", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "2.1", Name = "Pasivos Circulantes", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsSystem = true, IsActive = true, ParentId = GetTempId() },
            new() { Code = "2.1.01", Name = "Proveedores", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "2.1.02", Name = "IVA Débito Fiscal", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "2.1.03", Name = "ISR por Pagar", Type = AccountTypes.Liability, NormalSide = AccountSide.Credit, Level = 2, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },

            new() { Code = "3", Name = "Patrimonio", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "3.1.01", Name = "Capital Social", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "3.1.02", Name = "Utilidades Retenidas", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "3.1.03", Name = "Superávit por Revaluación", Type = AccountTypes.Equity, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },

            new() { Code = "4", Name = "Ingresos", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "4.1.01", Name = "Ventas", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "4.4.01", Name = "Ganancia en Venta de Activos", Type = AccountTypes.Income, NormalSide = AccountSide.Credit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },

            new() { Code = "5", Name = "Costos", Type = AccountTypes.Cost, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "5.1.01", Name = "Costo de Ventas", Type = AccountTypes.Cost, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },

            new() { Code = "6", Name = "Gastos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 0, CompanyId = companyId, IsSystem = true, IsActive = true },
            new() { Code = "6.1.01", Name = "Gastos Administrativos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "6.1.02", Name = "Gastos de Venta", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "6.1.04", Name = "Depreciación", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "6.1.05", Name = "Mantenimiento y Reparaciones", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
            new() { Code = "6.2.01", Name = "Pérdida en Baja de Activos", Type = AccountTypes.Expense, NormalSide = AccountSide.Debit, Level = 1, CompanyId = companyId, IsActive = true, ParentId = GetTempId() },
        };

        // Assign parent relationships properly
        var codeMap = new Dictionary<string, Account>();
        foreach (var a in accounts) codeMap[a.Code] = a;

        // Fix ParentId references based on code hierarchy
        foreach (var a in accounts)
        {
            if (a.ParentId == GetTempId())
            {
                var parentCode = a.Code.Length > 1 ? a.Code[..^a.Code.Split('.').Last().Length] : "";
                if (parentCode.EndsWith(".")) parentCode = parentCode[..^1];
                if (string.IsNullOrEmpty(parentCode))
                    a.ParentId = null;
                else if (codeMap.TryGetValue(parentCode, out var parent))
                    a.ParentId = parent.Id;
            }
        }

        foreach (var a in accounts)
            await _repo.AddAsync(a);
        await _repo.SaveChangesAsync();
    }

    private static Guid _tempId = Guid.NewGuid();
    private static Guid GetTempId() => _tempId;
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

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<AccountingEntryResponse> CreateManualEntryAsync(CreateManualEntryRequest request)
    {
        var period = await _periodRepo.GetByIdAsync(request.AccountingPeriodId)
            ?? throw new InvalidOperationException("Period not found");
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
            CompanyId = CompanyId,
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            PostedAt = DateTime.UtcNow,
            Details = request.Details.Select(d => new AccountingEntryDetail
            {
                AccountId = d.AccountId,
                DebitAmount = d.DebitAmount,
                CreditAmount = d.CreditAmount,
                Description = d.Description,
                CompanyId = CompanyId,
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
        var entry = await _entryRepo.GetByIdAsync(id) ?? throw new InvalidOperationException("Entry not found");
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

public sealed class AccountingPeriodService
{
    private readonly IAccountingPeriodRepository _repo;
    private readonly ITenantContext _tenant;

    public AccountingPeriodService(IAccountingPeriodRepository repo, ITenantContext tenant)
    { _repo = repo; _tenant = tenant; }

    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task<List<AccountingPeriodResponse>> GetAllAsync()
    {
        var periods = await _repo.GetAllAsync(CompanyId);
        return periods.Select(p => new AccountingPeriodResponse(p.Id, p.Year, p.Month, p.Name, p.Status, p.OpenedAt, p.ClosedAt)).ToList();
    }

    public async Task<AccountingPeriodResponse> OpenAsync(int year, int month)
    {
        var existing = await _repo.GetByYearMonthAsync(year, month, CompanyId);
        if (existing != null) throw new InvalidOperationException("Period already exists");

        var period = new AccountingPeriod
        {
            Year = year, Month = month, Name = $"{month:D2}-{year}",
            Status = "open", OpenedAt = DateTime.UtcNow, CompanyId = CompanyId
        };
        await _repo.AddAsync(period);
        await _repo.SaveChangesAsync();
        return new AccountingPeriodResponse(period.Id, period.Year, period.Month, period.Name, period.Status, period.OpenedAt, period.ClosedAt);
    }

    public async Task<AccountingPeriodResponse> CloseAsync(Guid id)
    {
        var period = await _repo.GetByIdAsync(id) ?? throw new InvalidOperationException("Period not found");
        period.Status = "closed";
        period.ClosedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(period);
        await _repo.SaveChangesAsync();
        return new AccountingPeriodResponse(period.Id, period.Year, period.Month, period.Name, period.Status, period.OpenedAt, period.ClosedAt);
    }
}
