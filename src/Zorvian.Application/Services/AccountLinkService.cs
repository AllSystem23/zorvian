using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class AccountLinkService
{
    private readonly IAccountLinkRepository _repo;
    private readonly IAccountRepository _accountRepo;
    private readonly ITenantContext _tenant;

    public AccountLinkService(IAccountLinkRepository repo, IAccountRepository accountRepo, ITenantContext tenant)
    { _repo = repo; _accountRepo = accountRepo; _tenant = tenant; }

    public Guid CompanyId => _tenant.RequireCompanyId();

    public async Task<List<AccountLinkResponse>> GetAllAsync()
    {
        var links = await _repo.GetByCompanyAsync(CompanyId);
        return links.Select(l => new AccountLinkResponse(l.Id, l.TransactionType, l.Role, l.AccountId, l.Account?.Code ?? "", l.Account?.Name ?? "")).ToList();
    }

    public async Task<AccountLinkResponse> CreateAsync(CreateAccountLinkRequest request)
    {
        var account = await _accountRepo.GetByIdAsync(request.AccountId)
            ?? throw new KeyNotFoundException("Account not found");

        var link = new AccountLink
        {
            TransactionType = request.TransactionType,
            Role = request.Role,
            AccountId = request.AccountId,
        };
        await _repo.AddAsync(link);
        await _repo.SaveChangesAsync();
        return new AccountLinkResponse(link.Id, link.TransactionType, link.Role, link.AccountId, account.Code, account.Name);
    }

    public async Task DeleteAsync(Guid id)
    {
        var links = await _repo.GetByCompanyAsync(CompanyId);
        var link = links.FirstOrDefault(l => l.Id == id)
            ?? throw new KeyNotFoundException("Link not found");
        await _repo.DeleteAsync(link);
        await _repo.SaveChangesAsync();
    }

    private async Task<Account?> ResolveAccountAsync(string shortCode, string nameKeywords, Guid companyId)
    {
        // 1. Exact short code match
        var account = await _accountRepo.GetByCodeAsync(shortCode, companyId);
        if (account != null) return account;

        // 2. Try variations: "1.1.01" -> "1.01.01", "1.01.01.000", "1.01.01.000.0000"
        var parts = shortCode.Split('.');
        if (parts.Length == 3)
        {
            var variants = new[]
            {
                $"{parts[0]}.{parts[1]}",
                $"{parts[0]}.0{parts[1]}.{parts[2]}",
                $"{parts[0]}.0{parts[1]}.0{parts[2]}",
                $"{parts[0]}.{parts[1]}.{parts[2]}.000",
                $"{parts[0]}.{parts[1]}.{parts[2]}.0000",
                $"{parts[0]}.0{parts[1]}.{parts[2]}.000",
                $"{parts[0]}.0{parts[1]}.{parts[2]}.0000",
                $"{parts[0]}.0{parts[1]}.0{parts[2]}.000",
                $"{parts[0]}.0{parts[1]}.0{parts[2]}.0000",
            };
            account = (await _accountRepo.GetByCodesAsync(variants, companyId)).FirstOrDefault();
            if (account != null) return account;
        }

        // 3. Last resort: fuzzy name match
        var allAccounts = await _accountRepo.GetAllAsync(companyId);
        var keywords = nameKeywords.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return allAccounts.FirstOrDefault(a =>
            keywords.Any(k => a.Name.Contains(k, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task SeedDefaultLinksAsync()
    {
        var existing = await _repo.GetByCompanyAsync(CompanyId);
        if (existing.Count > 0) return;

        var inventoryAccount = await ResolveAccountAsync("1.1.04", "Inventario", CompanyId);
        var clientsAccount = await ResolveAccountAsync("1.1.03", "Clientes", CompanyId);
        var cashAccount = await ResolveAccountAsync("1.1.01", "Caja", CompanyId);
        var vatReceivableAccount = await ResolveAccountAsync("1.1.05", "IVA Credito IVA Crédito", CompanyId);
        var suppliersAccount = await ResolveAccountAsync("2.1.01", "Proveedores", CompanyId);
        var vatPayableAccount = await ResolveAccountAsync("2.1.02", "IVA Debito IVA Débito", CompanyId);
        var salesAccount = await ResolveAccountAsync("4.1.01", "Ventas", CompanyId);
        var costAccount = await ResolveAccountAsync("5.1.01", "Costo", CompanyId);
        var retainedEarnings = await ResolveAccountAsync("3.1.02", "Utilidades Retenidas", CompanyId);

        var links = new List<AccountLink>();
        if (inventoryAccount != null)
        {
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "InventoryMovement", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "SupplierCreditNote", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
        }
        if (clientsAccount != null)
        {
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.AccountsReceivable, AccountId = clientsAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "Check", Role = AccountRoles.AccountsReceivable, AccountId = clientsAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "CreditPayment", Role = AccountRoles.AccountsReceivable, AccountId = clientsAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "Collection", Role = AccountRoles.AccountsReceivable, AccountId = clientsAccount.Id, CompanyId = CompanyId });
        }
        if (cashAccount != null)
        {
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "BankDeposit", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "SupplierPayment", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "CreditPayment", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "CashMovement", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "Collection", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
        }
        if (salesAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.SalesRevenue, AccountId = salesAccount.Id, CompanyId = CompanyId });
        if (costAccount != null)
        {
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.CostOfSales, AccountId = costAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "BankCommission", Role = AccountRoles.BankExpense, AccountId = costAccount.Id, CompanyId = CompanyId });
        }
        if (vatPayableAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.VatPayable, AccountId = vatPayableAccount.Id, CompanyId = CompanyId });
        if (suppliersAccount != null)
        {
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.AccountsPayable, AccountId = suppliersAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "SupplierPayment", Role = AccountRoles.AccountsPayable, AccountId = suppliersAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "SupplierCreditNote", Role = AccountRoles.AccountsPayable, AccountId = suppliersAccount.Id, CompanyId = CompanyId });
        }
        if (vatReceivableAccount != null)
        {
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.VatReceivable, AccountId = vatReceivableAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "SupplierCreditNote", Role = AccountRoles.VatReceivable, AccountId = vatReceivableAccount.Id, CompanyId = CompanyId });
        }
        if (inventoryAccount != null)
            links.Add(new() { TransactionType = "InventoryMovement", Role = AccountRoles.InventoryAdjustment, AccountId = inventoryAccount.Id, CompanyId = CompanyId });

        foreach (var link in links)
            await _repo.AddAsync(link);
        await _repo.SaveChangesAsync();
    }
}
