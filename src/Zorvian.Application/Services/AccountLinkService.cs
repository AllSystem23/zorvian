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

    public Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

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
            CompanyId = CompanyId,
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

    public async Task SeedDefaultLinksAsync()
    {
        var existing = await _repo.GetByCompanyAsync(CompanyId);
        if (existing.Count > 0) return;

        var inventoryAccount = await _accountRepo.GetByCodeAsync("1.1.04", CompanyId);
        var clientsAccount = await _accountRepo.GetByCodeAsync("1.1.03", CompanyId);
        var cashAccount = await _accountRepo.GetByCodeAsync("1.1.01", CompanyId);
        var vatReceivableAccount = await _accountRepo.GetByCodeAsync("1.1.05", CompanyId);
        var suppliersAccount = await _accountRepo.GetByCodeAsync("2.1.01", CompanyId);
        var vatPayableAccount = await _accountRepo.GetByCodeAsync("2.1.02", CompanyId);
        var salesAccount = await _accountRepo.GetByCodeAsync("4.1.01", CompanyId);
        var costAccount = await _accountRepo.GetByCodeAsync("5.1.01", CompanyId);
        var retainedEarnings = await _accountRepo.GetByCodeAsync("3.1.02", CompanyId);

        var links = new List<AccountLink>();
        if (inventoryAccount != null)
        {
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
            links.Add(new() { TransactionType = "InventoryMovement", Role = AccountRoles.Inventory, AccountId = inventoryAccount.Id, CompanyId = CompanyId });
        }
        if (clientsAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.AccountsReceivable, AccountId = clientsAccount.Id, CompanyId = CompanyId });
        if (cashAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.Cash, AccountId = cashAccount.Id, CompanyId = CompanyId });
        if (salesAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.SalesRevenue, AccountId = salesAccount.Id, CompanyId = CompanyId });
        if (costAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.CostOfSales, AccountId = costAccount.Id, CompanyId = CompanyId });
        if (vatPayableAccount != null)
            links.Add(new() { TransactionType = "Sale", Role = AccountRoles.VatPayable, AccountId = vatPayableAccount.Id, CompanyId = CompanyId });
        if (suppliersAccount != null)
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.AccountsPayable, AccountId = suppliersAccount.Id, CompanyId = CompanyId });
        if (vatReceivableAccount != null)
            links.Add(new() { TransactionType = "Purchase", Role = AccountRoles.VatReceivable, AccountId = vatReceivableAccount.Id, CompanyId = CompanyId });
        if (inventoryAccount != null)
            links.Add(new() { TransactionType = "InventoryMovement", Role = AccountRoles.InventoryAdjustment, AccountId = inventoryAccount.Id, CompanyId = CompanyId });

        foreach (var link in links)
            await _repo.AddAsync(link);
        await _repo.SaveChangesAsync();
    }
}
