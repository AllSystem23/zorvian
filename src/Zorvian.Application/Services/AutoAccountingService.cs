using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public interface IAutoAccountingService
{
    Task<Guid> GenerateSaleEntryAsync(Guid saleId, List<SaleDetail> details, decimal discount, decimal paidAmount, string saleType);
    Task<Guid> GenerateCostOfSaleEntryAsync(Guid saleId, decimal totalCost);
    Task<Guid> GeneratePurchaseEntryAsync(Guid purchaseId, List<PurchaseDetail> details, decimal discount, decimal total);
    Task<Guid> GenerateSupplierPaymentEntryAsync(Guid paymentId, Guid purchaseId, decimal amount, Guid companyId, Guid branchId);
    Task<Guid> GenerateSupplierCreditNoteEntryAsync(Guid creditNoteId, Guid supplierId, Guid? purchaseId, decimal total, Guid companyId, Guid branchId);
    Task<Guid> GenerateInventoryEntryAsync(Guid movementId, Guid productId, string movementType, int quantity, decimal unitCost);
    Task<Guid> GeneratePayrollEntryAsync(Guid payrollRunId);
    Task<Guid> GenerateCashMovementEntryAsync(Guid movementId);
}

public class AutoAccountingService : IAutoAccountingService
{
    private readonly IAccountingEntryRepository _entryRepo;
    private readonly IAccountingPeriodRepository _periodRepo;
    private readonly IAccountLinkRepository _linkRepo;
    private readonly IAccountingRuleRepository _ruleRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly ITenantContext _tenant;
    private readonly IPayrollRepository _payrollRepo;
    private readonly ICashMovementRepository _cashRepo;
public AutoAccountingService(
    IAccountingEntryRepository entryRepo,
    IAccountingPeriodRepository periodRepo,
    IAccountLinkRepository linkRepo,
    IAccountingRuleRepository ruleRepo,
    IAccountRepository accountRepo,
    ITenantContext tenant,
    IPayrollRepository payrollRepo,
    ICashMovementRepository cashRepo)
{
    _entryRepo = entryRepo; _periodRepo = periodRepo;
    _linkRepo = linkRepo; _ruleRepo = ruleRepo; _accountRepo = accountRepo; _tenant = tenant;
    _payrollRepo = payrollRepo;
    _cashRepo = cashRepo;
}
    private Guid CompanyId => Guid.TryParse(_tenant.TenantId, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    private async Task<Guid> GetPeriodIdAsync()
    {
        var now = DateTime.UtcNow;
        var period = await _periodRepo.GetCurrentOpenAsync(CompanyId);
        if (period != null) return period.Id;

        period = await _periodRepo.GetByYearMonthAsync(now.Year, now.Month, CompanyId);
        if (period != null)
        {
            if (period.Status == "closed") throw new InvalidOperationException($"Period {period.Name} is closed");
            return period.Id;
        }

        period = new AccountingPeriod
        {
            Year = now.Year, Month = now.Month, Name = $"{now.Month:00}-{now.Year}",
            Status = "open", OpenedAt = DateTime.UtcNow, CompanyId = CompanyId
        };
        await _periodRepo.AddAsync(period);
        await _periodRepo.SaveChangesAsync();
        return period.Id;
    }

    private async Task<Guid> GetAccountIdAsync(string transactionType, string role)
    {
        var link = await _linkRepo.GetByTransactionTypeAndRoleAsync(transactionType, role, CompanyId);
        if (link != null) return link.AccountId;

        link = await _linkRepo.GetByTransactionTypeAndRoleAsync("*", role, CompanyId);
        return link?.AccountId ?? throw new InvalidOperationException($"No account linked for {transactionType}/{role}");
    }
    
    private async Task<Guid> GetAccountIdByCodeAsync(string code)
    {
        var account = await _accountRepo.GetByCodeAsync(code, CompanyId);
        return account?.Id ?? throw new InvalidOperationException($"Account not found for code: {code}");
    }

    public async Task<Guid> GenerateSaleEntryAsync(Guid saleId, List<SaleDetail> details, decimal discount, decimal paidAmount, string saleType)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        var groupedDetails = details
            .GroupBy(d => d.Product.TaxCategory)
            .Select(g => new {
                TaxCategory = g.Key,
                Subtotal = g.Sum(d => d.Subtotal),
                Discount = g.Sum(d => d.Discount),
                Tax = g.Sum(d => (d.Subtotal - d.Discount) * (g.Key?.Rate ?? 0))
            }).ToList();

        var totalSubtotal = groupedDetails.Sum(g => g.Subtotal);
        var totalDiscount = groupedDetails.Sum(g => g.Discount);
        var totalTax = groupedDetails.Sum(g => g.Tax);
        var total = (totalSubtotal - totalDiscount) + totalTax;

        var arAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.AccountsReceivable);
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Cash);

        var entryDetails = new List<AccountingEntryDetail>();

        if (saleType == "cash")
        {
            entryDetails.Add(new() { AccountId = cashAccountId, DebitAmount = total, CreditAmount = 0, Description = "Venta al contado" });
        }
        else
        {
            entryDetails.Add(new() { AccountId = arAccountId, DebitAmount = total, CreditAmount = 0, Description = "Venta al crédito" });
        }

        foreach (var group in groupedDetails)
        {
            var salesAccountCode = group.TaxCategory?.SalesAccountCode ?? "4.1.01";
            var vatAccountCode = group.TaxCategory?.VatAccountCode ?? "2.1.02";

            var salesAccountId = await GetAccountIdByCodeAsync(salesAccountCode);
            var vatAccountId = await GetAccountIdByCodeAsync(vatAccountCode);

            entryDetails.Add(new() { AccountId = salesAccountId, DebitAmount = 0, CreditAmount = (group.Subtotal - group.Discount), Description = $"Ventas {group.TaxCategory?.Name}" });
            entryDetails.Add(new() { AccountId = vatAccountId, DebitAmount = 0, CreditAmount = group.Tax, Description = $"IVA {group.TaxCategory?.Name}" });
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Venta {(saleType == "cash" ? "Contado" : "Crédito")} #{saleId.ToString()[..8]}",
            ReferenceType = "Sale",
            ReferenceId = saleId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails.Select(d => { d.CompanyId = companyId; return d; }).ToList(),
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCostOfSaleEntryAsync(Guid saleId, decimal totalCost)
    {
        var periodId = await GetPeriodIdAsync();
        var costAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.CostOfSales);
        var invAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Inventory);

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Costo de Venta #{saleId.ToString()[..8]}",
            ReferenceType = "Sale",
            ReferenceId = saleId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = CompanyId,
            TotalDebit = totalCost,
            TotalCredit = totalCost,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { AccountId = costAccountId, DebitAmount = totalCost, CreditAmount = 0, Description = "Costo de venta", CompanyId = CompanyId },
                new() { AccountId = invAccountId, DebitAmount = 0, CreditAmount = totalCost, Description = "Salida de inventario", CompanyId = CompanyId },
            ],
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GeneratePurchaseEntryAsync(Guid purchaseId, List<PurchaseDetail> details, decimal discount, decimal total)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        var groupedDetails = details
            .GroupBy(d => d.Product.TaxCategory)
            .Select(g => new {
                TaxCategory = g.Key,
                Subtotal = g.Sum(d => d.Subtotal),
                Tax = g.Sum(d => d.Subtotal * (g.Key?.Rate ?? 0))
            }).ToList();

        var apAccountId = await GetAccountIdAsync(TransactionTypes.Purchase, AccountRoles.AccountsPayable);

        var entryDetails = new List<AccountingEntryDetail>();

        entryDetails.Add(new() { AccountId = apAccountId, DebitAmount = 0, CreditAmount = total, Description = "Compra a proveedor" });

        foreach (var group in groupedDetails)
        {
            var invAccountCode = group.TaxCategory?.SalesAccountCode ?? "1.1.04";
            var vatAccountCode = group.TaxCategory?.VatAccountCode ?? "1.1.05";

            var invAccountId = await GetAccountIdByCodeAsync(invAccountCode);
            var vatAccountId = await GetAccountIdByCodeAsync(vatAccountCode);

            entryDetails.Add(new() { AccountId = invAccountId, DebitAmount = group.Subtotal, CreditAmount = 0, Description = $"Inventario {group.TaxCategory?.Name}" });
            entryDetails.Add(new() { AccountId = vatAccountId, DebitAmount = group.Tax, CreditAmount = 0, Description = $"IVA Crédito {group.TaxCategory?.Name}" });
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Compra #{purchaseId.ToString()[..8]}",
            ReferenceType = "Purchase",
            ReferenceId = purchaseId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails.Select(d => { d.CompanyId = companyId; return d; }).ToList(),
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateInventoryEntryAsync(Guid movementId, Guid productId, string movementType, int quantity, decimal unitCost)
    {
        var periodId = await GetPeriodIdAsync();
        var invAccountId = await GetAccountIdAsync(TransactionTypes.InventoryMovement, AccountRoles.Inventory);
        var adjAccountId = await GetAccountIdAsync(TransactionTypes.InventoryMovement, AccountRoles.InventoryAdjustment);

        var amount = quantity * unitCost;
        AccountingEntryDetail detail1, detail2;

        if (movementType == "entry" || movementType == "adjustment_positive" || movementType == "purchase")
        {
            detail1 = new() { AccountId = invAccountId, DebitAmount = amount, CreditAmount = 0, Description = "Entrada de inventario", CompanyId = CompanyId };
            detail2 = new() { AccountId = adjAccountId, DebitAmount = 0, CreditAmount = amount, Description = "Ajuste de inventario", CompanyId = CompanyId };
        }
        else
        {
            detail1 = new() { AccountId = adjAccountId, DebitAmount = amount, CreditAmount = 0, Description = "Ajuste de inventario", CompanyId = CompanyId };
            detail2 = new() { AccountId = invAccountId, DebitAmount = 0, CreditAmount = amount, Description = "Salida de inventario", CompanyId = CompanyId };
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Movimiento Inventario: {movementType} #{movementId.ToString()[..8]}",
            ReferenceType = "InventoryMovement",
            ReferenceId = movementId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = CompanyId,
            TotalDebit = amount,
            TotalCredit = amount,
            PostedAt = DateTime.UtcNow,
            Details = [detail1, detail2],
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GeneratePayrollEntryAsync(Guid payrollRunId)
    {
        var payrollRun = await _payrollRepo.GetRunByIdAsync(payrollRunId) ?? throw new InvalidOperationException("Run not found");
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        var entryDetails = new List<AccountingEntryDetail>();

        var accountTotals = new Dictionary<Guid, (decimal Debit, decimal Credit)>();

        void AddDetail(Guid accountId, decimal debit, decimal credit, string description)
        {
            var totals = accountTotals.GetValueOrDefault(accountId);
            accountTotals[accountId] = (totals.Debit + debit, totals.Credit + credit);
            entryDetails.Add(new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = description });
        }

        var salaryExpAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "SalaryExpense");
        var overtimeExpAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "OvertimeExpense");
        var employerInssExpAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "EmployerInssExpense");

        foreach (var detail in payrollRun.Details)
        {
            AddDetail(salaryExpAcc, detail.BaseSalary, 0, $"Salario base emp {detail.EmployeeId}");
            
            foreach (var concept in detail.Concepts)
            {
                if (concept.IsEmployerCost)
                {
                    var accId = concept.ConceptCode == "INSS_PAT" ? employerInssExpAcc : await GetAccountIdAsync(TransactionTypes.Payroll, "OtherEmployerExpense");
                    AddDetail(accId, concept.Amount, 0, concept.Description);
                }
                else if (concept.ConceptCode != "SALARY")
                {
                    var accId = concept.ConceptCode == "OVERTIME" ? overtimeExpAcc : await GetAccountIdAsync(TransactionTypes.Payroll, "OtherEarningExpense");
                    AddDetail(accId, concept.Amount, 0, concept.Description);
                }
            }
        }

        var inssEmpPayableAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "EmployeeInssPayable");
        var irPayableAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "IrWithholdingPayable");
        var inssPatPayableAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "EmployerInssPayable");
        var bankAcc = await GetAccountIdAsync(TransactionTypes.Payroll, "NetPayCash");

        AddDetail(inssEmpPayableAcc, 0, payrollRun.Details.Sum(d => d.InssDeduction ?? 0), "INSS laboral");
        AddDetail(irPayableAcc, 0, payrollRun.Details.Sum(d => d.IrDeduction ?? 0), "IR retenido");
        AddDetail(inssPatPayableAcc, 0, payrollRun.TotalEmployerCosts, "INSS patronal por pagar");
        AddDetail(bankAcc, 0, payrollRun.TotalNetPay, "Neto a pagar");

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Nómina #{payrollRunId.ToString()[..8]}",
            ReferenceType = "Payroll",
            ReferenceId = payrollRunId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails.Select(d => { d.CompanyId = companyId; return d; }).ToList(),
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCashMovementEntryAsync(Guid movementId)
    {
        var movement = await _cashRepo.GetByIdAsync(movementId)
            ?? throw new InvalidOperationException("Movement not found");

        var periodId = await GetPeriodIdAsync();
        
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.Cash);
        var contraAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.ContraAccount);

        var isIncome = movement.MovementType == "Income";
        
        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"{movement.MovementType}: {movement.Concept} #{movementId.ToString()[..8]}",
            ReferenceType = "CashMovement",
            ReferenceId = movementId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = CompanyId,
            TotalDebit = isIncome ? movement.Amount : 0,
            TotalCredit = isIncome ? 0 : movement.Amount,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { 
                    AccountId = isIncome ? cashAccountId : contraAccountId, 
                    DebitAmount = isIncome ? movement.Amount : 0, 
                    CreditAmount = isIncome ? 0 : movement.Amount, 
                    Description = movement.Concept, CompanyId = CompanyId 
                },
                new() { 
                    AccountId = isIncome ? contraAccountId : cashAccountId, 
                    DebitAmount = isIncome ? 0 : movement.Amount, 
                    CreditAmount = isIncome ? movement.Amount : 0, 
                    Description = movement.Concept, CompanyId = CompanyId 
                },
            ],
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateSupplierPaymentEntryAsync(Guid paymentId, Guid purchaseId, decimal amount, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var apAccountId = await GetAccountIdAsync(TransactionTypes.SupplierPayment, AccountRoles.AccountsPayable);
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.SupplierPayment, AccountRoles.Cash);

        if (cashAccountId == Guid.Empty)
            cashAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.Cash);

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Pago a proveedor #{purchaseId.ToString()[..8]}",
            ReferenceType = "SupplierPayment",
            ReferenceId = paymentId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = amount,
            TotalCredit = amount,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { AccountId = apAccountId, DebitAmount = amount, CreditAmount = 0, Description = "Pago a proveedor", CompanyId = companyId },
                new() { AccountId = cashAccountId, DebitAmount = 0, CreditAmount = amount, Description = "Salida de efectivo", CompanyId = companyId },
            ],
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateSupplierCreditNoteEntryAsync(Guid creditNoteId, Guid supplierId, Guid? purchaseId, decimal total, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var apAccountId = await GetAccountIdAsync(TransactionTypes.SupplierCreditNote, AccountRoles.AccountsPayable);
        var invAccountId = await GetAccountIdAsync(TransactionTypes.SupplierCreditNote, AccountRoles.Inventory);
        var vatAccountId = await GetAccountIdAsync(TransactionTypes.SupplierCreditNote, AccountRoles.VatReceivable);

        var description = purchaseId.HasValue
            ? $"Nota crédito proveedor #{purchaseId.Value.ToString()[..8]}"
            : $"Nota crédito proveedor #{supplierId.ToString()[..8]}";

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = description,
            ReferenceType = "SupplierCreditNote",
            ReferenceId = creditNoteId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = total,
            TotalCredit = total,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { AccountId = apAccountId, DebitAmount = total, CreditAmount = 0, Description = "Nota crédito proveedor", CompanyId = companyId },
                new() { AccountId = invAccountId, DebitAmount = 0, CreditAmount = total, Description = "Devolución de inventario", CompanyId = companyId },
            ],
        };

        await _entryRepo.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    private async Task<string> GenerateNumberAsync()
    {
        var count = await _entryRepo.GetFilteredCountAsync(null, null, null, null, null, CompanyId);
        return $"AS-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
