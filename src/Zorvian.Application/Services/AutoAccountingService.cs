using System.Text.Json;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class AutoAccountingService : IAutoAccountingService
{
    private readonly IAccountingRuleTemplateRepository _repository;
    private readonly IAccountingEntryRepository? _entryRepo;
    private readonly IAccountingPeriodRepository? _periodRepo;
    private readonly IAccountLinkRepository? _linkRepo;
    private readonly IAccountingRuleRepository? _ruleRepo;
    private readonly IAccountRepository? _accountRepo;
    private readonly ITenantContext? _tenant;
    private readonly IPayrollRepository? _payrollRepo;
    private readonly ICashMovementRepository? _cashRepo;

    public AutoAccountingService(IAccountingRuleTemplateRepository repository)
    {
        _repository = repository;
    }

    // Constructor for legacy integration tests
    public AutoAccountingService(
        IAccountingEntryRepository entryRepo,
        IAccountingPeriodRepository periodRepo,
        IAccountLinkRepository linkRepo,
        IAccountingRuleRepository ruleRepo,
        IAccountRepository accountRepo,
        ITenantContext tenant,
        IPayrollRepository payrollRepo,
        ICashMovementRepository cashRepo,
        IAccountingRuleTemplateRepository templateRepo)
    {
        _entryRepo = entryRepo;
        _periodRepo = periodRepo;
        _linkRepo = linkRepo;
        _ruleRepo = ruleRepo;
        _accountRepo = accountRepo;
        _tenant = tenant;
        _payrollRepo = payrollRepo;
        _cashRepo = cashRepo;
        _repository = templateRepo;
    }

    private Guid CompanyId => Guid.TryParse(_tenant?.TenantId?.ToString() ?? string.Empty, out var id) ? id : throw new InvalidOperationException("Invalid tenant");

    public async Task GenerateEntryAsync(string processTrigger, Guid companyId, string countryCode, object context)
    {
        var template = await _repository.GetTemplateAsync(processTrigger, companyId, countryCode);
        if (template == null) return;

        var rules = JsonSerializer.Deserialize<JsonElement>(template.EntryStructureJson);
        if (rules.ValueKind == JsonValueKind.Undefined) return;
    }

    private async Task<Guid> GetPeriodIdAsync()
    {
        var now = DateTime.UtcNow;
        var period = await _periodRepo!.GetCurrentOpenAsync(CompanyId);
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
        var link = await _linkRepo!.GetByTransactionTypeAndRoleAsync(transactionType, role, CompanyId);
        if (link != null) return link.AccountId;

        link = await _linkRepo.GetByTransactionTypeAndRoleAsync("*", role, CompanyId);
        return link?.AccountId ?? throw new InvalidOperationException($"No account linked for {transactionType}/{role}");
    }

    private async Task<Guid> GetAccountIdByCodeAsync(string code)
    {
        var account = await _accountRepo!.GetByCodeAsync(code, CompanyId);
        return account?.Id ?? throw new InvalidOperationException($"Account not found for code: {code}");
    }

    private decimal EvaluateFormula(string? formula, object context)
    {
        if (string.IsNullOrEmpty(formula)) return 0;
        var properties = context.GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (formula.Equals(prop.Name, StringComparison.OrdinalIgnoreCase))
            {
                var value = prop.GetValue(context);
                return value is decimal d ? d : 0m;
            }
        }
        return 0;
    }

    public async Task<Guid> GenerateSaleEntryAsync(Guid saleId, List<SaleDetail> details, decimal discount, decimal paidAmount, string saleType, Guid? costCenterId = null)
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

        var context = new { Total = total, Subtotal = totalSubtotal, Tax = totalTax, Discount = totalDiscount, PaidAmount = paidAmount };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.Sale, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var amount = EvaluateFormula(rule.Formula, context);
                if (amount <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.Sale, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);

                entryDetails.Add(MakeDetail(accountId, isDebit ? amount : 0, isDebit ? 0 : amount, $"Sale {rule.AccountRole}"));
            }
        }
        else
        {
            var arAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.AccountsReceivable);
            var cashAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Cash);

            if (saleType == "cash")
                entryDetails.Add(MakeDetail(cashAccountId, total, 0, "Venta al contado"));
            else
                entryDetails.Add(MakeDetail(arAccountId, total, 0, "Venta al crédito"));

            foreach (var group in groupedDetails)
            {
                var salesAccountCode = group.TaxCategory?.SalesAccountCode ?? "4.1.01";
                var vatAccountCode = group.TaxCategory?.VatAccountCode ?? "2.1.02";

                var salesAccountId = await GetAccountIdByCodeAsync(salesAccountCode);
                var vatAccountId = await GetAccountIdByCodeAsync(vatAccountCode);

                entryDetails.Add(MakeDetail(salesAccountId, 0, group.Subtotal - group.Discount, $"Ventas {group.TaxCategory?.Name}"));
                entryDetails.Add(MakeDetail(vatAccountId, 0, group.Tax, $"IVA {group.TaxCategory?.Name}"));
            }
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
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCostOfSaleEntryAsync(Guid saleId, decimal totalCost, Guid? costCenterId = null)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        var context = new { TotalCost = totalCost };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.CostOfSale, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var amount = EvaluateFormula(rule.Formula, context);
                if (amount <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.Sale, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? amount : 0, isDebit ? 0 : amount, $"CostOfSale {rule.AccountRole}"));
            }
        }
        else
        {
            var costAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.CostOfSales);
            var invAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Inventory);
            entryDetails.Add(MakeDetail(costAccountId, totalCost, 0, "Costo de venta"));
            entryDetails.Add(MakeDetail(invAccountId, 0, totalCost, "Salida de inventario"));
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Costo de Venta #{saleId.ToString()[..8]}",
            ReferenceType = "Sale",
            ReferenceId = saleId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
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

        var totalSubtotal = groupedDetails.Sum(g => g.Subtotal);
        var totalTax = groupedDetails.Sum(g => g.Tax);

        var context = new { Total = total, Subtotal = totalSubtotal, Tax = totalTax, Discount = discount };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.Purchase, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var amount = EvaluateFormula(rule.Formula, context);
                if (amount <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.Purchase, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? amount : 0, isDebit ? 0 : amount, $"Purchase {rule.AccountRole}"));
            }
        }
        else
        {
            var apAccountId = await GetAccountIdAsync(TransactionTypes.Purchase, AccountRoles.AccountsPayable);
            entryDetails.Add(MakeDetail(apAccountId, 0, total, "Compra a proveedor"));

            foreach (var group in groupedDetails)
            {
                var invAccountCode = group.TaxCategory?.SalesAccountCode ?? "1.1.04";
                var vatAccountCode = group.TaxCategory?.VatAccountCode ?? "1.1.05";

                var invAccountId = await GetAccountIdByCodeAsync(invAccountCode);
                var vatAccountId = await GetAccountIdByCodeAsync(vatAccountCode);

                entryDetails.Add(MakeDetail(invAccountId, group.Subtotal, 0, $"Inventario {group.TaxCategory?.Name}"));
                entryDetails.Add(MakeDetail(vatAccountId, group.Tax, 0, $"IVA Crédito {group.TaxCategory?.Name}"));
            }
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
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task ReversePurchaseEntryAsync(Guid purchaseId, List<PurchaseDetail> details, decimal total)
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

        var totalSubtotal = groupedDetails.Sum(g => g.Subtotal);
        var totalTax = groupedDetails.Sum(g => g.Tax);

        var context = new { Total = total, Subtotal = totalSubtotal, Tax = totalTax };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.PurchaseReversal, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var amount = EvaluateFormula(rule.Formula, context);
                if (amount <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.Purchase, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? amount : 0, isDebit ? 0 : amount, $"PurchaseReversal {rule.AccountRole}"));
            }
        }
        else
        {
            var apAccountId = await GetAccountIdAsync(TransactionTypes.Purchase, AccountRoles.AccountsPayable);
            entryDetails.Add(MakeDetail(apAccountId, total, 0, "Anulación compra - proveedor"));

            foreach (var group in groupedDetails)
            {
                var invAccountCode = group.TaxCategory?.SalesAccountCode ?? "1.1.04";
                var vatAccountCode = group.TaxCategory?.VatAccountCode ?? "1.1.05";

                var invAcctId = await GetAccountIdByCodeAsync(invAccountCode);
                entryDetails.Add(MakeDetail(invAcctId, 0, group.Subtotal, "Anulación compra - inventario"));

                if (group.Tax > 0)
                {
                    var vatAcctId = await GetAccountIdByCodeAsync(vatAccountCode);
                    entryDetails.Add(MakeDetail(vatAcctId, 0, group.Tax, "Anulación compra - IVA"));
                }
            }
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Anulación Compra #{purchaseId.ToString()[..8]}",
            ReferenceType = "PurchaseReversal",
            ReferenceId = purchaseId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
    }

    public async Task<Guid> GenerateInventoryEntryAsync(Guid movementId, Guid productId, string movementType, decimal quantity, decimal unitCost)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;
        var amount = quantity * unitCost;

        var context = new { Amount = amount, Quantity = quantity, UnitCost = unitCost };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.InventoryMovement, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var val = EvaluateFormula(rule.Formula, context);
                if (val <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.InventoryMovement, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? val : 0, isDebit ? 0 : val, $"Inventory {rule.AccountRole}"));
            }
        }
        else
        {
            var invAccountId = await GetAccountIdAsync(TransactionTypes.InventoryMovement, AccountRoles.Inventory);
            var adjAccountId = await GetAccountIdAsync(TransactionTypes.InventoryMovement, AccountRoles.InventoryAdjustment);

            if (movementType == "entry" || movementType == "adjustment_positive" || movementType == "purchase")
            {
                entryDetails.Add(MakeDetail(invAccountId, amount, 0, "Entrada de inventario"));
                entryDetails.Add(MakeDetail(adjAccountId, 0, amount, "Ajuste de inventario"));
            }
            else
            {
                entryDetails.Add(MakeDetail(adjAccountId, amount, 0, "Ajuste de inventario"));
                entryDetails.Add(MakeDetail(invAccountId, 0, amount, "Salida de inventario"));
            }
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
            CompanyId = companyId,
            TotalDebit = amount,
            TotalCredit = amount,
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCreditPaymentEntryAsync(Guid paymentId, Guid creditId, decimal principal, decimal interest, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var totalAmount = principal + interest;

        var context = new { Total = totalAmount, Principal = principal, Interest = interest };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.CreditPayment, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var val = EvaluateFormula(rule.Formula, context);
                if (val <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.CreditPayment, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? val : 0, isDebit ? 0 : val, $"CreditPayment {rule.AccountRole}"));
            }
        }
        else
        {
            var cashAccountId = await GetAccountIdAsync(TransactionTypes.CreditPayment, AccountRoles.Cash);
            var arAccountId = await GetAccountIdAsync(TransactionTypes.CreditPayment, AccountRoles.AccountsReceivable);
            var interestAccountId = await GetAccountIdAsync(TransactionTypes.CreditPayment, AccountRoles.InterestIncome);

            entryDetails.Add(MakeDetail(cashAccountId, totalAmount, 0, "Ingreso de efectivo"));
            entryDetails.Add(MakeDetail(arAccountId, 0, principal, "Pago de capital"));
            entryDetails.Add(MakeDetail(interestAccountId, 0, interest, "Intereses ganados"));
        }

        var entry = new AccountingEntry
        {
            Id = Guid.NewGuid(),
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Pago de crédito #{creditId.ToString()[..8]}",
            ReferenceType = "CreditPayment",
            ReferenceId = paymentId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = totalAmount,
            TotalCredit = totalAmount,
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GeneratePayrollEntryAsync(Guid payrollRunId)
    {
        var payrollRun = await _payrollRepo!.GetRunByIdAsync(payrollRunId) ?? throw new InvalidOperationException("Run not found");
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

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCashMovementEntryAsync(Guid movementId)
    {
        var movement = await _cashRepo!.GetByIdAsync(movementId)
            ?? throw new InvalidOperationException("Movement not found");

        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        var context = new { Amount = movement.Amount };
        var entryDetails = new List<AccountingEntryDetail>();

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var rules = await _ruleRepo!.GetByEventTypeAsync(TransactionTypes.CashMovement, companyId);
        if (rules != null && rules.Any())
        {
            foreach (var rule in rules.OrderBy(r => r.SortOrder))
            {
                var val = EvaluateFormula(rule.Formula, context);
                if (val <= 0) continue;

                var accountId = await GetAccountIdAsync(TransactionTypes.CashMovement, rule.AccountRole);
                var isDebit = rule.LineType.Equals("Debit", StringComparison.OrdinalIgnoreCase);
                entryDetails.Add(MakeDetail(accountId, isDebit ? val : 0, isDebit ? 0 : val, $"CashMovement {rule.AccountRole}"));
            }
        }
        else
        {
            var cashAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.Cash);
            var contraAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.ContraAccount);

            var isIncome = movement.MovementType.Equals("Income", StringComparison.OrdinalIgnoreCase);

            entryDetails.Add(new()
            {
                AccountId = isIncome ? cashAccountId : contraAccountId,
                DebitAmount = isIncome ? movement.Amount : 0,
                CreditAmount = isIncome ? 0 : movement.Amount,
                Description = movement.Concept,
                CompanyId = companyId
            });
            entryDetails.Add(new()
            {
                AccountId = isIncome ? contraAccountId : cashAccountId,
                DebitAmount = isIncome ? 0 : movement.Amount,
                CreditAmount = isIncome ? movement.Amount : 0,
                Description = movement.Concept,
                CompanyId = companyId
            });
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"{movement.MovementType}: {movement.Concept} #{movementId.ToString()[..8]}",
            ReferenceType = "CashMovement",
            ReferenceId = movementId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = movement.Amount,
            TotalCredit = movement.Amount,
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
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

        await _entryRepo!.AddAsync(entry);
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

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateFixedAssetAcquisitionEntryAsync(Guid assetId, decimal cost, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var assetAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetAcquisition, AccountRoles.FixedAssetCost);
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetAcquisition, AccountRoles.Cash);

        if (cashAccountId == Guid.Empty)
            cashAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.Cash);

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Adquisición activo fijo #{assetId.ToString()[..8]}",
            ReferenceType = "FixedAssetAcquisition",
            ReferenceId = assetId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = cost,
            TotalCredit = cost,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { AccountId = assetAccountId, DebitAmount = cost, CreditAmount = 0, Description = "Costo activo fijo", CompanyId = companyId },
                new() { AccountId = cashAccountId, DebitAmount = 0, CreditAmount = cost, Description = "Salida de efectivo", CompanyId = companyId },
            ],
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateDepreciationEntryAsync(Guid entryId, decimal amount, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var deprExpenseId = await GetAccountIdAsync(TransactionTypes.FixedAssetDepreciation, AccountRoles.DepreciationExpense);
        var accumDeprId = await GetAccountIdAsync(TransactionTypes.FixedAssetDepreciation, AccountRoles.AccumulatedDepreciation);

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Depreciación activo fijo #{entryId.ToString()[..8]}",
            ReferenceType = "FixedAssetDepreciation",
            ReferenceId = entryId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = amount,
            TotalCredit = amount,
            PostedAt = DateTime.UtcNow,
            Details =
            [
                new() { AccountId = deprExpenseId, DebitAmount = amount, CreditAmount = 0, Description = "Gasto depreciación", CompanyId = companyId },
                new() { AccountId = accumDeprId, DebitAmount = 0, CreditAmount = amount, Description = "Depreciación acumulada", CompanyId = companyId },
            ],
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateDisposalEntryAsync(Guid disposalId, decimal acquisitionCost, decimal accumulatedDep, decimal saleAmount, decimal gainOrLoss, string disposalType, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var assetAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetDisposal, AccountRoles.FixedAssetCost);
        var accumDeprId = await GetAccountIdAsync(TransactionTypes.FixedAssetDisposal, AccountRoles.AccumulatedDepreciation);
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetDisposal, AccountRoles.Cash);
        var gainAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetDisposal, AccountRoles.GainOnDisposal);
        var lossAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetDisposal, AccountRoles.LossOnDisposal);

        if (cashAccountId == Guid.Empty)
            cashAccountId = await GetAccountIdAsync(TransactionTypes.CashMovement, AccountRoles.Cash);

        var isGain = gainOrLoss >= 0;

        var details = new List<AccountingEntryDetail>
        {
            new() { AccountId = accumDeprId, DebitAmount = 0, CreditAmount = accumulatedDep, Description = "Baja depreciación acumulada", CompanyId = companyId },
            new() { AccountId = assetAccountId, DebitAmount = 0, CreditAmount = acquisitionCost, Description = "Baja costo activo", CompanyId = companyId },
        };

        if (saleAmount > 0)
        {
            details.Add(new() { AccountId = cashAccountId, DebitAmount = saleAmount, CreditAmount = 0, Description = "Venta activo fijo", CompanyId = companyId });
        }

        if (isGain && gainOrLoss > 0)
        {
            details.Add(new() { AccountId = gainAccountId, DebitAmount = 0, CreditAmount = gainOrLoss, Description = "Ganancia en venta de activo", CompanyId = companyId });
        }
        else if (!isGain && gainOrLoss < 0)
        {
            details.Add(new() { AccountId = lossAccountId, DebitAmount = Math.Abs(gainOrLoss), CreditAmount = 0, Description = "Pérdida en baja de activo", CompanyId = companyId });
        }

        var totalDebit = details.Sum(d => d.DebitAmount);
        var totalCredit = details.Sum(d => d.CreditAmount);
        var maxTotal = Math.Max(totalDebit, totalCredit);

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Baja activo fijo #{disposalId.ToString()[..8]}",
            ReferenceType = "FixedAssetDisposal",
            ReferenceId = disposalId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = maxTotal,
            TotalCredit = maxTotal,
            PostedAt = DateTime.UtcNow,
            Details = details,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateRevaluationEntryAsync(Guid revaluationId, decimal previousValue, decimal newValue, decimal accumulatedDep, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var assetAccountId = await GetAccountIdAsync(TransactionTypes.FixedAssetRevaluation, AccountRoles.FixedAssetCost);
        var revalSurplusId = await GetAccountIdAsync(TransactionTypes.FixedAssetRevaluation, AccountRoles.RevaluationSurplus);

        var difference = newValue - previousValue;
        var isIncrease = difference >= 0;

        var entry = new AccountingEntry
        {
            EntryNumber = $"AS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4]}",
            EntryDate = DateTime.UtcNow,
            Description = $"Revaluación activo fijo #{revaluationId.ToString()[..8]}",
            ReferenceType = "FixedAssetRevaluation",
            ReferenceId = revaluationId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            TotalDebit = isIncrease ? difference : Math.Abs(difference),
            TotalCredit = isIncrease ? difference : Math.Abs(difference),
            PostedAt = DateTime.UtcNow,
            Details =
            [
                isIncrease
                    ? new() { AccountId = assetAccountId, DebitAmount = difference, CreditAmount = 0, Description = "Incremento por revaluación", CompanyId = companyId }
                    : new() { AccountId = assetAccountId, DebitAmount = 0, CreditAmount = Math.Abs(difference), Description = "Decremento por revaluación", CompanyId = companyId },
                isIncrease
                    ? new() { AccountId = revalSurplusId, DebitAmount = 0, CreditAmount = difference, Description = "Superávit por revaluación", CompanyId = companyId }
                    : new() { AccountId = revalSurplusId, DebitAmount = Math.Abs(difference), CreditAmount = 0, Description = "Reversión superávit por revaluación", CompanyId = companyId },
            ],
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateWarrantyCostEntryAsync(Guid costId, string category, decimal totalCost, string paidBy, Guid? paidByPartyId, Guid warrantyId, Guid companyId, Guid branchId)
    {
        var periodId = await GetPeriodIdAsync();
        var expenseRole = category switch
        {
            "parts" => AccountRoles.WarrantyPartsExpense,
            "labor" => AccountRoles.WarrantyLaborExpense,
            _ => AccountRoles.WarrantyExpense,
        };

        var expenseAccountId = await GetAccountIdAsync(TransactionTypes.WarrantyCost, expenseRole);
        var creditAccountId = paidBy == "provider"
            ? await GetAccountIdAsync(TransactionTypes.WarrantyCost, AccountRoles.WarrantyProviderReceivable)
            : await GetAccountIdAsync(TransactionTypes.WarrantyCost, AccountRoles.Cash);

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Costo de garantía {(paidBy == "provider" ? "a cargo del proveedor" : "por la empresa")} - {category}",
            ReferenceType = "WarrantyCost",
            ReferenceId = costId,
            Status = "posted",
            TotalDebit = totalCost,
            TotalCredit = totalCost,
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            BranchId = branchId,
            Details =
            [
                new() { AccountId = expenseAccountId, DebitAmount = totalCost, CreditAmount = 0, Description = category, CompanyId = companyId },
                new() { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = totalCost, Description = paidBy == "provider" ? "Cta x Cobrar proveedor" : "Caja/Banco", CompanyId = companyId },
            ],
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCreditNoteEntryAsync(Guid creditNoteId, Guid saleId, string saleType, List<CreditNoteDetail> details, decimal subtotal, decimal tax)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;
        var total = subtotal + tax;

        var incomeAccountCode = "4.1.01";
        var vatAccountCode = "2.1.02";
        var costAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.CostOfSales);
        var invAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Inventory);

        var taxCategory = details.FirstOrDefault()?.Product?.TaxCategory;
        if (taxCategory != null)
        {
            incomeAccountCode = taxCategory.SalesAccountCode ?? incomeAccountCode;
            vatAccountCode = taxCategory.VatAccountCode ?? vatAccountCode;
        }

        var salesAccountId = await GetAccountIdByCodeAsync(incomeAccountCode);
        var vatAccountId = await GetAccountIdByCodeAsync(vatAccountCode);

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId };
        }

        var totalCost = details.Sum(d => d.Quantity * (d.Product?.CostPrice ?? 0));
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(salesAccountId, subtotal, 0, "Reversión venta - ingresos"),
            MakeDetail(vatAccountId, tax, 0, "Reversión venta - IVA"),
            MakeDetail(invAccountId, totalCost, 0, "Reversión inventario"),
            MakeDetail(costAccountId, 0, totalCost, "Reversión costo de venta"),
        };

        var cashAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Cash);
        var arAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.AccountsReceivable);

        if (saleType == "cash")
            entryDetails.Add(MakeDetail(cashAccountId, 0, total, "Reversión venta - efectivo"));
        else
            entryDetails.Add(MakeDetail(arAccountId, 0, total, "Reversión venta - cuenta por cobrar"));

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Nota de Crédito #{creditNoteId.ToString()[..8]}",
            ReferenceType = "CreditNote",
            ReferenceId = creditNoteId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCheckEntryAsync(Guid checkId, decimal amount, string checkType, Guid? bankAccountId, Guid? payeeId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var entryDetails = new List<AccountingEntryDetail>();
        var bankAccId = bankAccountId ?? await GetAccountIdAsync(TransactionTypes.Check, AccountRoles.Bank);

        switch (checkType.ToLowerInvariant())
        {
            case "issuance":
            case "payment":
                var apAccountId = await GetAccountIdAsync(TransactionTypes.Check, AccountRoles.AccountsPayable);
                entryDetails.Add(MakeDetail(apAccountId, amount, 0, $"Emisión cheque #{checkId.ToString()[..8]}"));
                entryDetails.Add(MakeDetail(bankAccId, 0, amount, "Cargo bancario por cheque"));
                break;
            case "cancellation":
                var canceledApId = await GetAccountIdAsync(TransactionTypes.Check, AccountRoles.AccountsPayable);
                var canceledBankId = await GetAccountIdAsync(TransactionTypes.Check, AccountRoles.Bank);
                entryDetails.Add(MakeDetail(canceledBankId, amount, 0, $"Cancelación cheque #{checkId.ToString()[..8]}"));
                entryDetails.Add(MakeDetail(canceledApId, 0, amount, "Reversión CxP por cancelación cheque"));
                break;
            case "reconciliation":
                var reclassAccId = await GetAccountIdByCodeAsync("1.1.03");
                entryDetails.Add(MakeDetail(reclassAccId, amount, 0, $"Conciliación cheque #{checkId.ToString()[..8]}"));
                entryDetails.Add(MakeDetail(bankAccId, 0, amount, "Ajuste conciliación bancaria"));
                break;
            default:
                throw new ArgumentException($"Unknown check type: {checkType}");
        }

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Cheque {(checkType == "issuance" ? "Emitido" : checkType == "payment" ? "Pago" : checkType == "cancellation" ? "Cancelado" : "Conciliado")} #{checkId.ToString()[..8]}",
            ReferenceType = "Check",
            ReferenceId = checkId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateBankDepositEntryAsync(Guid depositId, decimal amount, Guid bankAccountId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var cashAccountId = await GetAccountIdByCodeAsync("1.1.01");
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(bankAccountId, amount, 0, "Depósito bancario"),
            MakeDetail(cashAccountId, 0, amount, "Salida de efectivo por depósito"),
        };

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Depósito Bancario #{depositId.ToString()[..8]}",
            ReferenceType = "BankDeposit",
            ReferenceId = depositId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateBankTransferEntryAsync(Guid transferId, decimal amount, Guid fromAccountId, Guid toAccountId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(fromAccountId, 0, amount, "Transferencia bancaria - origen"),
            MakeDetail(toAccountId, amount, 0, "Transferencia bancaria - destino"),
        };

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Transferencia Bancaria #{transferId.ToString()[..8]}",
            ReferenceType = "BankTransfer",
            ReferenceId = transferId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateBankCommissionEntryAsync(Guid commissionId, decimal commission, Guid bankAccountId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var bankExpenseAccountId = await GetAccountIdByCodeAsync("5.1.01");
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(bankExpenseAccountId, commission, 0, "Comisión bancaria"),
            MakeDetail(bankAccountId, 0, commission, "Cargo por comisión bancaria"),
        };

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Comisión Bancaria #{commissionId.ToString()[..8]}",
            ReferenceType = "BankCommission",
            ReferenceId = commissionId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateCollectionEntryAsync(Guid collectionId, decimal amount, decimal interest, decimal lateFee, Guid invoiceId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var cashAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.Cash);
        var arAccountId = await GetAccountIdAsync(TransactionTypes.Sale, AccountRoles.AccountsReceivable);
        var interestIncomeAccountId = await GetAccountIdByCodeAsync("4.3.01");
        var lateFeeIncomeAccountId = await GetAccountIdByCodeAsync("4.3.02");

        var total = amount + interest + lateFee;
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(cashAccountId, total, 0, $"Cobranza factura #{invoiceId.ToString()[..8]}"),
            MakeDetail(arAccountId, 0, amount, "Aplicación a cuenta por cobrar"),
        };

        if (interest > 0)
            entryDetails.Add(MakeDetail(interestIncomeAccountId, 0, interest, "Intereses por cobranza"));

        if (lateFee > 0)
            entryDetails.Add(MakeDetail(lateFeeIncomeAccountId, 0, lateFee, "Recargos por mora"));

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Cobranza Recibo #{collectionId.ToString()[..8]}",
            ReferenceType = "Collection",
            ReferenceId = collectionId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateAdvanceToSupplierEntryAsync(Guid advanceId, decimal amount, Guid supplierId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var advancesAccountId = await GetAccountIdByCodeAsync("1.1.06");
        var cashAccountId = await GetAccountIdAsync(TransactionTypes.Purchase, AccountRoles.Cash);
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(advancesAccountId, amount, 0, $"Anticipo a proveedor #{supplierId.ToString()[..8]}"),
            MakeDetail(cashAccountId, 0, amount, "Pago de anticipo"),
        };

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Anticipo a Proveedor #{advanceId.ToString()[..8]}",
            ReferenceType = "AdvanceToSupplier",
            ReferenceId = advanceId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    public async Task<Guid> GenerateSupplierAdvanceApplicationEntryAsync(Guid applicationId, decimal amount, Guid advanceId, Guid purchaseId, Guid? costCenterId)
    {
        var periodId = await GetPeriodIdAsync();
        var companyId = CompanyId;

        AccountingEntryDetail MakeDetail(Guid accountId, decimal debit, decimal credit, string desc)
        {
            return new() { AccountId = accountId, DebitAmount = debit, CreditAmount = credit, Description = desc, CompanyId = companyId, CostCenterId = costCenterId };
        }

        var advancesAccountId = await GetAccountIdByCodeAsync("1.1.06");
        var apAccountId = await GetAccountIdAsync(TransactionTypes.Purchase, AccountRoles.AccountsPayable);
        var entryDetails = new List<AccountingEntryDetail>
        {
            MakeDetail(apAccountId, amount, 0, $"Aplicación anticipo a compra #{purchaseId.ToString()[..8]}"),
            MakeDetail(advancesAccountId, 0, amount, "Cancelación anticipo"),
        };

        var entry = new AccountingEntry
        {
            EntryNumber = await GenerateNumberAsync(),
            EntryDate = DateTime.UtcNow,
            Description = $"Aplicación Anticipo #{advanceId.ToString()[..8]} a Compra #{purchaseId.ToString()[..8]}",
            ReferenceType = "AdvanceApplication",
            ReferenceId = applicationId,
            Status = "posted",
            AccountingPeriodId = periodId,
            CompanyId = companyId,
            CostCenterId = costCenterId,
            TotalDebit = entryDetails.Sum(d => d.DebitAmount),
            TotalCredit = entryDetails.Sum(d => d.CreditAmount),
            PostedAt = DateTime.UtcNow,
            Details = entryDetails,
        };

        await _entryRepo!.AddAsync(entry);
        await _entryRepo.SaveChangesAsync();
        return entry.Id;
    }

    private async Task<string> GenerateNumberAsync()
    {
        var count = await _entryRepo!.GetFilteredCountAsync(null, null, null, null, null, CompanyId);
        return $"AS-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
    }
}
