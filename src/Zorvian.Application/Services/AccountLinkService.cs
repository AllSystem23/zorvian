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

    /// <summary>Semeilla (o completa) los enlaces de cuentas por defecto. Semántica
    /// ensure-missing: solo agrega los pares (TransactionType, Role) que aún no existen,
    /// por lo que es idempotente y sirve de ruta de upgrade para compañías existentes
    /// (ejecutable de nuevo vía POST /account-links/seed). Los pares cuya cuenta no se
    /// resuelve se omiten sin error (plan contable personalizado: los configura el contador).</summary>
    public async Task SeedDefaultLinksAsync()
    {
        var existing = await _repo.GetByCompanyAsync(CompanyId);
        var existingPairs = existing
            .Select(l => $"{l.TransactionType}|{l.Role}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inventoryAccount = await ResolveAccountAsync("1.1.04", "Inventario", CompanyId);
        var clientsAccount = await ResolveAccountAsync("1.1.03", "Clientes", CompanyId);
        var cashAccount = await ResolveAccountAsync("1.1.01", "Caja", CompanyId);
        var vatReceivableAccount = await ResolveAccountAsync("1.1.05", "IVA Credito IVA Crédito", CompanyId);
        var suppliersAccount = await ResolveAccountAsync("2.1.01", "Proveedores", CompanyId);
        var vatPayableAccount = await ResolveAccountAsync("2.1.02", "IVA Debito IVA Débito", CompanyId);
        var salesAccount = await ResolveAccountAsync("4.1.01", "Ventas", CompanyId);
        var costAccount = await ResolveAccountAsync("5.1.01", "Costo", CompanyId);
        var retainedEarnings = await ResolveAccountAsync("3.1.02", "Utilidades Retenidas", CompanyId);

        // ── Cuentas de nómina, prestadores de servicios, INSS y adelantos ──
        // (sembradas por AccountService.SeedDefaultChartOfAccountsAsync; en plan contables
        // personalizado la resolución cae a fuzzy-name y, si no hay match, se omite el par).
        var sueldosAccount = await ResolveAccountAsync("6.1.03", "Sueldos", CompanyId);
        var inssPatronalExpAccount = await ResolveAccountAsync("6.1.06", "INSS", CompanyId);
        var horasExtraAccount = await ResolveAccountAsync("6.1.07", "Horas Extra", CompanyId);
        var beneficiosAccount = await ResolveAccountAsync("6.1.08", "Beneficios", CompanyId);
        var honorariosAccount = await ResolveAccountAsync("6.1.09", "Honorarios", CompanyId);
        var gastosAdminAccount = await ResolveAccountAsync("6.1.01", "Gastos Administrativos", CompanyId);
        var inssLaboralPagar = await ResolveAccountAsync("2.1.04", "INSS Laboral", CompanyId);
        var inssPatronalPagar = await ResolveAccountAsync("2.1.05", "INSS Patronal por Pagar", CompanyId);
        var isrPagarAccount = await ResolveAccountAsync("2.1.03", "ISR", CompanyId);
        var adelantosAccount = await ResolveAccountAsync("1.1.06", "Adelantos", CompanyId);

        var links = new List<AccountLink>();
        void Add(string transactionType, string role, Account? account)
        {
            if (account is null) return;
            var key = $"{transactionType}|{role}";
            if (existingPairs.Contains(key)) return;
            if (links.Any(l => l.TransactionType == transactionType && string.Equals(l.Role, role, StringComparison.OrdinalIgnoreCase))) return;
            links.Add(new() { TransactionType = transactionType, Role = role, AccountId = account.Id, CompanyId = CompanyId });
        }
        if (inventoryAccount != null)
        {
            Add("Sale", AccountRoles.Inventory, inventoryAccount);
            Add("Purchase", AccountRoles.Inventory, inventoryAccount);
            Add("InventoryMovement", AccountRoles.Inventory, inventoryAccount);
            Add("SupplierCreditNote", AccountRoles.Inventory, inventoryAccount);
            Add("InventoryMovement", AccountRoles.InventoryAdjustment, inventoryAccount);
        }
        if (clientsAccount != null)
        {
            Add("Sale", AccountRoles.AccountsReceivable, clientsAccount);
            Add("Check", AccountRoles.AccountsReceivable, clientsAccount);
            Add("CreditPayment", AccountRoles.AccountsReceivable, clientsAccount);
            Add("Collection", AccountRoles.AccountsReceivable, clientsAccount);
        }
        if (cashAccount != null)
        {
            Add("Sale", AccountRoles.Cash, cashAccount);
            Add("BankDeposit", AccountRoles.Cash, cashAccount);
            Add("SupplierPayment", AccountRoles.Cash, cashAccount);
            Add("CreditPayment", AccountRoles.Cash, cashAccount);
            Add("CashMovement", AccountRoles.Cash, cashAccount);
            Add("Collection", AccountRoles.Cash, cashAccount);
            // Nómina: neto a pagar en efectivo. Prestadores: efectivo saliente del comprobante.
            Add(TransactionTypes.Payroll, "NetPayCash", cashAccount);
            Add(TransactionTypes.ServiceProviderPayment, AccountRoles.Cash, cashAccount);
        }
        if (salesAccount != null)
            Add("Sale", AccountRoles.SalesRevenue, salesAccount);
        if (costAccount != null)
        {
            Add("Sale", AccountRoles.CostOfSales, costAccount);
            Add("BankCommission", AccountRoles.BankExpense, costAccount);
        }
        if (vatPayableAccount != null)
            Add("Sale", AccountRoles.VatPayable, vatPayableAccount);
        if (suppliersAccount != null)
        {
            Add("Purchase", AccountRoles.AccountsPayable, suppliersAccount);
            Add("SupplierPayment", AccountRoles.AccountsPayable, suppliersAccount);
            Add("SupplierCreditNote", AccountRoles.AccountsPayable, suppliersAccount);
        }
        if (vatReceivableAccount != null)
        {
            Add("Purchase", AccountRoles.VatReceivable, vatReceivableAccount);
            Add("SupplierCreditNote", AccountRoles.VatReceivable, vatReceivableAccount);
        }

        // ── Nómina (PayrollService → GeneratePayrollEntryAsync al aprobar la corrida) ──
        Add(TransactionTypes.Payroll, "SalaryExpense", sueldosAccount);
        Add(TransactionTypes.Payroll, "OvertimeExpense", horasExtraAccount ?? sueldosAccount);
        Add(TransactionTypes.Payroll, "EmployerInssExpense", inssPatronalExpAccount);
        Add(TransactionTypes.Payroll, "OtherEmployerExpense", beneficiosAccount ?? sueldosAccount);
        Add(TransactionTypes.Payroll, "OtherEarningExpense", horasExtraAccount ?? sueldosAccount);
        Add(TransactionTypes.Payroll, "EmployeeInssPayable", inssLaboralPagar);
        Add(TransactionTypes.Payroll, "EmployerInssPayable", inssPatronalPagar);
        Add(TransactionTypes.Payroll, "IrWithholdingPayable", isrPagarAccount);

        // ── Pago a Prestadores de Servicios (GenerateServiceProviderPaymentEntryAsync al pagar) ──
        Add(TransactionTypes.ServiceProviderPayment, AccountRoles.ServicesExpense, honorariosAccount ?? gastosAdminAccount);
        Add(TransactionTypes.ServiceProviderPayment, AccountRoles.Advances, adelantosAccount);
        Add(TransactionTypes.ServiceProviderPayment, AccountRoles.WithholdingPayable, inssLaboralPagar);

        foreach (var link in links)
            await _repo.AddAsync(link);
        if (links.Count > 0)
            await _repo.SaveChangesAsync();
    }
}
