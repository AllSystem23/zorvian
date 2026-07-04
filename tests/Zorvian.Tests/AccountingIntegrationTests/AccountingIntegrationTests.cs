using Microsoft.EntityFrameworkCore;
using Moq;
using Zorvian.Application.DTOs.CashRegister;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.DTOs.Credit;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Xunit;

namespace Zorvian.Tests.AccountingIntegrationTests;

public sealed class AccountingIntegrationTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly AutoAccountingService _autoAccounting;
    private readonly Mock<IApprovalEngine> _approvalEngine = new();

    public AccountingIntegrationTests()
    {
        var tenantId = _companyId.ToString();
        _tenant.Setup(t => t.TenantId).Returns(tenantId);
        _tenant.Setup(t => t.CurrentUserId).Returns(Guid.NewGuid());

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase("AccountingIntegration_" + Guid.NewGuid())
            .AddInterceptors(new AuditInterceptor(_tenant.Object))
            .Options;
        _db = new ZorvianDbContext(options, _tenant.Object);

        SeedAccounts();
        SeedAccountLinks();

        var entryRepo = new EntryRepo(_db, _companyId);
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        periodRepo.Setup(r => r.GetCurrentOpenAsync(_companyId))
            .ReturnsAsync(new AccountingPeriod { Id = Guid.NewGuid(), Status = "open" });

        var linkRepo = new LinkRepo(_db);
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new AccRepo(_db);
        var payrollRepo = new Mock<IPayrollRepository>();

        _autoAccounting = new AutoAccountingService(
            new EntryRepo(_db, _companyId), periodRepo.Object, new LinkRepo(_db), ruleRepo.Object,
            new AccRepo(_db), _tenant.Object, payrollRepo.Object, new CashMovementRepo(_db), new AccountingRuleTemplateRepository(_db), new CompanyRepository(_db), new Mock<IFiscalYearRepository>().Object, new Mock<ICountryTaxConfigRepository>().Object);

        _approvalEngine.Setup(e => e.EvaluateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new Zorvian.Application.DTOs.Approval.ApprovalEvaluationResult(false, null, null));
    }

    [Fact]
    public async Task CashSale_FullCycle_ShouldGenerateSaleAndCostEntries()
    {
        var client = SeedClient();
        var product = SeedProduct(SeedTaxCategory(0.15m));
        var saleRepo = new SaleRepo(_db);
        var productRepo = new ProductRepo(_db);
        var movementRepo = new MovementRepo(_db);
        var clientRepo = new ClientRepo(_db);

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<Sale>(It.IsAny<CreateCashSaleRequest>()))
            .Returns(new Sale { TenantId = _companyId.ToString(), CompanyId = _companyId });
        mapper.Setup(m => m.Map<SaleResponse>(It.IsAny<Sale>()))
            .Returns<Sale>(s => new SaleResponse(
                s.Id, s.InvoiceNumber ?? "", s.ClientId, "", Guid.Empty, "",
                s.SaleDate, s.SaleType, s.Subtotal, s.Tax, s.Discount, s.Total,
                s.PaidAmount, s.Balance, s.Status ?? "", s.Notes,
                "NIO", null, new List<SaleDetailItem>(), null));

        var periodRepo = new Mock<IAccountingPeriodRepository>();
        periodRepo.Setup(r => r.GetCurrentOpenAsync(_companyId))
            .ReturnsAsync(new AccountingPeriod { Id = Guid.NewGuid(), Status = "open" });

        var saleService = new SaleService(
            saleRepo, productRepo, movementRepo, new Mock<ICompanyRepository>().Object,
            clientRepo, new Mock<ICreditRepository>().Object,
            _autoAccounting, new Mock<IWebhookService>().Object, _tenant.Object, mapper.Object, new Mock<IGoalIntegrationService>().Object,
            periodRepo.Object);

        var result = await saleService.CreateCashSaleAsync(new CreateCashSaleRequest(
            client.Id, Guid.NewGuid(), 0, null, _branchId,
            "NIO", null, new List<SaleDetailItem> { new(product.Id, "P1", 5, 100, 0, 500) },
            new SalePaymentInfo(575, "cash", "REF", Guid.NewGuid())));

        Assert.NotNull(result);
        var entries = _db.Set<AccountingEntry>()
            .Include(e => e.Details)
            .Where(e => e.ReferenceId == result.Id)
            .ToList();
        Assert.Equal(2, entries.Count);
        foreach (var entry in entries)
        {
            Assert.Equal("Sale", entry.ReferenceType);
            Assert.Equal(entry.TotalDebit, entry.TotalCredit);
        }
    }

    [Fact]
    public async Task CreditSale_FullCycle_ShouldGenerateSaleEntry()
    {
        var product = SeedProduct(SeedTaxCategory(0.15m));
        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "FAC-001",
            ClientId = SeedClient(creditLimit: 50000m).Id,
            SaleDate = DateTime.UtcNow,
            SaleType = "credit",
            Subtotal = 1000,
            Tax = 150,
            Discount = 0,
            Total = 1150,
            PaidAmount = 200,
            Balance = 950,
            Status = "pending",
            TenantId = _companyId.ToString(),
            CompanyId = _companyId,
            BranchId = _branchId,
        };
        var saleDetail = new SaleDetail
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ProductId = product.Id,
            Product = product,
            Quantity = 10,
            UnitPrice = 100,
            Discount = 0,
            Subtotal = 1000,
            TenantId = _companyId.ToString(),
            CompanyId = _companyId,
        };
        _db.Set<Sale>().Add(sale);
        _db.Set<SaleDetail>().Add(saleDetail);
        await _db.SaveChangesAsync();

        var entryId = await _autoAccounting.GenerateSaleEntryAsync(
            sale.Id, [saleDetail], 0, 200, "credit");

        VerifyAccountingEntries(sale.Id, "Sale");
    }

    [Fact]
    public async Task CreditPayment_FullCycle_ShouldGeneratePaymentEntry()
    {
        var client = SeedClient();
        var credit = SeedCredit(client.Id);

        var creditRepo = new Mock<ICreditRepository>();
        creditRepo.Setup(r => r.GetByIdAsync(credit.Id))
            .ReturnsAsync(() => _db.Set<Credit>().Include(c => c.Installments).Include(c => c.Payments).FirstOrDefault(c => c.Id == credit.Id));
        creditRepo.Setup(r => r.SaveChangesAsync())
            .Returns(() => _db.SaveChangesAsync());

        var paymentRepo = new Mock<ICreditPaymentRepository>();
        CreditPayment? capturedPayment = null;
        paymentRepo.Setup(r => r.AddAsync(It.IsAny<CreditPayment>()))
            .Callback<CreditPayment>(p => { capturedPayment = p; _db.Set<CreditPayment>().Add(p); })
            .Returns(Task.CompletedTask);
        paymentRepo.Setup(r => r.SaveChangesAsync())
            .Returns(() => _db.SaveChangesAsync());

        var mapper = new Mock<AutoMapper.IMapper>();
        mapper.Setup(m => m.Map<CreditPaymentResponse>(It.IsAny<CreditPayment>()))
            .Returns<CreditPayment>(p => new CreditPaymentResponse(
                p.Id, p.CreditId, p.CreditInstallmentId, p.Amount,
                p.PrincipalAmount, p.InterestAmount, p.PaymentMethod,
                p.ReferenceNumber, p.PaymentDate, null));

        var creditService = new CreditService(
            creditRepo.Object, paymentRepo.Object,
            new Mock<ILateFeeRepository>().Object,
            new Mock<ICollectionActionRepository>().Object,
            new Mock<ICreditRefinancingRepository>().Object,
            new Mock<ICompanyRepository>().Object,
            new Mock<ISaleRepository>().Object,
            _autoAccounting, _tenant.Object, mapper.Object);

        var result = await creditService.RegisterPaymentAsync(new CreateCreditPaymentRequest(
            credit.Id, null, 500, "cash", "PAY001", null));

        Assert.NotNull(result);
        VerifyAccountingEntries(result.Id, "CreditPayment");
    }

    [Fact]
    public async Task Purchase_FullCycle_ShouldGeneratePurchaseEntry()
    {
        var supplier = SeedSupplier();
        var product = SeedProduct(SeedTaxCategory(0.15m));

        var purchaseRepo = new PurchaseRepo(_db);
        var productRepo = new ProductRepo(_db);
        var supplierRepo = new SupplierRepo(_db);
        var movementRepo = new MovementRepo(_db);

        var companyRepoMock = new Mock<ICompanyRepository>();
        companyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Company { Id = _companyId, Country = "Nicaragua" });
        companyRepoMock.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CompanySettings?)null);

        var purchaseService = new PurchaseService(
            purchaseRepo, productRepo, movementRepo, companyRepoMock.Object,
            supplierRepo, _autoAccounting, new Mock<IWebhookService>().Object, _tenant.Object,
            new Mock<AutoMapper.IMapper>().Object, _approvalEngine.Object);

        var result = await purchaseService.CreateAsync(new CreatePurchaseRequest(
            supplier.Id, DateTime.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            "INV-001", null, null, 0, null, _branchId,
            "NIO", null, null, new List<PurchaseDetailItem> { new(product.Id, "P1", 20, 50, 0, 1000) }));

        Assert.NotNull(result);
        VerifyAccountingEntries(result.Id, "Purchase");
    }

    [Fact]
    public async Task PurchaseCancel_FullCycle_ShouldGenerateReversalEntry()
    {
        var supplier = SeedSupplier();
        var product = SeedProduct(SeedTaxCategory(0.15m));

        var purchaseRepo = new PurchaseRepo(_db);
        var productRepo = new ProductRepo(_db);
        var supplierRepo = new SupplierRepo(_db);
        var movementRepo = new MovementRepo(_db);

        var companyRepoMock = new Mock<ICompanyRepository>();
        companyRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Company { Id = _companyId, Country = "Nicaragua" });
        companyRepoMock.Setup(r => r.GetSettingsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((CompanySettings?)null);

        var purchaseService = new PurchaseService(
            purchaseRepo, productRepo, movementRepo, companyRepoMock.Object,
            supplierRepo, _autoAccounting, new Mock<IWebhookService>().Object, _tenant.Object,
            new Mock<AutoMapper.IMapper>().Object, _approvalEngine.Object);

        var purchase = await purchaseService.CreateAsync(new CreatePurchaseRequest(
            supplier.Id, DateTime.UtcNow, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            "INV-002", null, null, 0, null, _branchId,
            "NIO", null, null, new List<PurchaseDetailItem> { new(product.Id, "P1", 10, 100, 0, 1000) }));
        Assert.NotNull(purchase);

        var cancelled = await purchaseService.CancelAsync(purchase.Id);
        Assert.NotNull(cancelled);
        Assert.Equal("cancelled", cancelled.Status);

        VerifyAccountingEntries(purchase.Id, "Purchase", "PurchaseReversal");
    }

    [Fact]
    public async Task CashMovementApproval_FullCycle_ShouldGenerateMovementEntry()
    {
        var register = SeedCashRegister();
        var movement = SeedCashMovement(register.Id);

        var registerRepo = new Mock<ICashRegisterRepository>();
        registerRepo.Setup(r => r.GetByIdAsync(register.Id)).ReturnsAsync(register);
        registerRepo.Setup(r => r.SaveChangesAsync()).Returns(() => _db.SaveChangesAsync());

        var movementRepo = new Mock<ICashMovementRepository>();
        movementRepo.Setup(r => r.GetByIdAsync(movement.Id)).ReturnsAsync(movement);
        movementRepo.Setup(r => r.UpdateAsync(It.IsAny<CashMovement>()))
            .Callback<CashMovement>(m => { m.ApprovalStatus = "approved"; _db.Set<CashMovement>().Update(m); })
            .Returns(Task.CompletedTask);
        movementRepo.Setup(r => r.SaveChangesAsync()).Returns(() => _db.SaveChangesAsync());

        var cashService = new CashRegisterService(
            registerRepo.Object, movementRepo.Object, new Mock<ICashRegisterArqueoRepository>().Object, _tenant.Object,
            new Mock<AutoMapper.IMapper>().Object, _autoAccounting);

        var result = await cashService.ApproveMovementAsync(movement.Id);
        Assert.True(result);

        VerifyAccountingEntries(movement.Id, "CashMovement");
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private void VerifyAccountingEntries(Guid referenceId, params string[] expectedTypes)
    {
        var entries = _db.Set<AccountingEntry>()
            .Include(e => e.Details)
            .Where(e => e.ReferenceId == referenceId)
            .ToList();

        foreach (var type in expectedTypes)
            Assert.Contains(entries, e => e.ReferenceType == type);

        foreach (var entry in entries)
            Assert.Equal(entry.TotalDebit, entry.TotalCredit);

        Assert.True(entries.Count >= expectedTypes.Length);
    }

    private void SeedAccounts()
    {
        _db.Set<Account>().AddRange(
            new Account { Id = Guid.NewGuid(), Code = "1101", Name = "Caja", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "1201", Name = "Cuentas por Cobrar", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "1301", Name = "Inventario", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "2101", Name = "Cuentas por Pagar", Type = "Liability", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "4101", Name = "Ingresos por Ventas", Type = "Revenue", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "5101", Name = "Costo de Ventas", Type = "Expense", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "6101", Name = "IVA por Cobrar", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "6102", Name = "IVA por Pagar", Type = "Liability", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "3301", Name = "Intereses por Cobrar", Type = "Revenue", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "4.1.01", Name = "Ventas", Type = "Revenue", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "2.1.02", Name = "IVA Débito Fiscal", Type = "Liability", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "1.1.04", Name = "Inventario Compras", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new Account { Id = Guid.NewGuid(), Code = "1.1.05", Name = "IVA Crédito Fiscal", Type = "Asset", IsActive = true, TenantId = _companyId.ToString(), CompanyId = _companyId });
        _db.SaveChanges();
    }

    private void SeedAccountLinks()
    {
        var cash = _db.Set<Account>().First(a => a.Code == "1101");
        var ar = _db.Set<Account>().First(a => a.Code == "1201");
        var inv = _db.Set<Account>().First(a => a.Code == "1301");
        var ap = _db.Set<Account>().First(a => a.Code == "2101");
        var rev = _db.Set<Account>().First(a => a.Code == "4101");
        var cogs = _db.Set<Account>().First(a => a.Code == "5101");
        var interest = _db.Set<Account>().First(a => a.Code == "3301");

        _db.Set<AccountLink>().AddRange(
            new AccountLink { TransactionType = "Sale", Role = "Cash", AccountId = cash.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "Sale", Role = "AccountsReceivable", AccountId = ar.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "Sale", Role = "CostOfSales", AccountId = cogs.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "Sale", Role = "Inventory", AccountId = inv.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "Purchase", Role = "AccountsPayable", AccountId = ap.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "Purchase", Role = "Inventory", AccountId = inv.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "PurchaseReversal", Role = "AccountsPayable", AccountId = ap.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "PurchaseReversal", Role = "Inventory", AccountId = inv.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "CreditPayment", Role = "Cash", AccountId = cash.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "CreditPayment", Role = "AccountsReceivable", AccountId = ar.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "CreditPayment", Role = "InterestIncome", AccountId = interest.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "CashMovement", Role = "Cash", AccountId = cash.Id, TenantId = _companyId.ToString(), CompanyId = _companyId },
            new AccountLink { TransactionType = "CashMovement", Role = "ContraAccount", AccountId = rev.Id, TenantId = _companyId.ToString(), CompanyId = _companyId });
        _db.SaveChanges();
    }

    private TaxCategory SeedTaxCategory(decimal rate)
    {
        var tc = new TaxCategory { Id = Guid.NewGuid(), Name = $"Tax{rate:P0}", Rate = rate,
            SalesAccountCode = "4.1.01", VatAccountCode = "2.1.02",
            TenantId = _companyId.ToString(), CompanyId = _companyId };
        _db.Set<TaxCategory>().Add(tc);
        _db.SaveChanges();
        return tc;
    }

    private Client SeedClient(decimal? creditLimit = null)
    {
        var c = new Client { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Client",
            IdentificationNumber = "12345678",
            TenantId = _companyId.ToString(), CompanyId = _companyId, BranchId = _branchId, CreditLimit = creditLimit };
        _db.Set<Client>().Add(c);
        _db.SaveChanges();
        return c;
    }

    private Product SeedProduct(TaxCategory? tc = null)
    {
        var p = new Product { Id = Guid.NewGuid(), Name = "Test", Code = "P001",
            Stock = 100, CostPrice = 50, SellingPrice = 100, TenantId = _companyId.ToString(), CompanyId = _companyId, TaxCategory = tc };
        _db.Set<Product>().Add(p);
        _db.SaveChanges();
        return p;
    }

    private Supplier SeedSupplier()
    {
        var s = new Supplier { Id = Guid.NewGuid(), Name = "Test Supplier", TenantId = _companyId.ToString(), CompanyId = _companyId };
        _db.Set<Supplier>().Add(s);
        _db.SaveChanges();
        return s;
    }

    private Credit SeedCredit(Guid clientId)
    {
        var credit = new Credit { Id = Guid.NewGuid(), CreditNumber = "CRE-001",
            ClientId = clientId, FinancedAmount = 1000, TotalAmount = 1200,
            PaidAmount = 0, Balance = 1200, InstallmentCount = 6,
            InstallmentAmount = 200, InterestRate = 5, InterestAmount = 200,
            Status = "active", TenantId = _companyId.ToString(), CompanyId = _companyId, BranchId = _branchId };
        _db.Set<Credit>().Add(credit);
        _db.SaveChanges();
        return credit;
    }

    private CashRegister SeedCashRegister()
    {
        var r = new CashRegister { Id = Guid.NewGuid(), Code = "CAJA-001",
            BranchId = _branchId, TenantId = _companyId.ToString(), CompanyId = _companyId };
        _db.Set<CashRegister>().Add(r);
        _db.SaveChanges();
        return r;
    }

    private CashMovement SeedCashMovement(Guid registerId)
    {
        var m = new CashMovement { Id = Guid.NewGuid(), CashRegisterId = registerId,
            Amount = 1000, MovementType = "Income", Concept = "Test",
            ApprovalStatus = "pending", TenantId = _companyId.ToString(), CompanyId = _companyId, BranchId = _branchId };
        _db.Set<CashMovement>().Add(m);
        _db.SaveChanges();
        return m;
    }

    private sealed class EntryRepo(ZorvianDbContext db, Guid tenantId) : IAccountingEntryRepository
    {
        public Task AddAsync(AccountingEntry entry)
        {
            if (string.IsNullOrEmpty(entry.TenantId)) entry.TenantId = tenantId.ToString();
            if (entry.Details is not null)
                foreach (var d in entry.Details)
                    if (string.IsNullOrEmpty(d.TenantId)) d.TenantId = tenantId.ToString();
            db.Set<AccountingEntry>().Add(entry);
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task<List<AccountingEntry>> GetListByIdsAsync(IEnumerable<Guid> ids) => db.Set<AccountingEntry>().Include(e => e.Details).Where(e => ids.Contains(e.Id)).ToListAsync();
        public Task<AccountingEntry?> GetByIdAsync(Guid id) => db.Set<AccountingEntry>().Include(e => e.Details).FirstOrDefaultAsync(e => e.Id == id);
        public Task UpdateAsync(AccountingEntry entry) { db.Set<AccountingEntry>().Update(entry); return Task.CompletedTask; }
        public Task<string> GenerateEntryNumberAsync(Guid companyId) => Task.FromResult("AS-" + Guid.NewGuid().ToString("N")[..8]);
        public Task<bool> HasEntriesForAccountAsync(Guid accountId) => db.Set<AccountingEntry>().AnyAsync(e => e.Details.Any(d => d.AccountId == accountId));
        public Task<List<AccountingEntry>> GetPostedWithDetailsAsync(Guid? periodId, Guid companyId, DateTime? toDate = null)
        {
            var query = db.Set<AccountingEntry>()
                .Include(e => e.Details)
                .Where(e => e.CompanyId == companyId && e.Status == "posted")
                .AsQueryable();
            if (periodId.HasValue) query = query.Where(e => e.AccountingPeriodId == periodId.Value);
            if (toDate.HasValue) query = query.Where(e => e.EntryDate < toDate.Value);
            return query.ToListAsync();
        }
        public Task<List<AccountingEntry>> GetFilteredAsync(Guid? companyId, string? status, string? referenceType, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(Guid? companyId, string? status, string? referenceType, DateTime? fromDate, DateTime? toDate, Guid branchId)
        {
            var query = db.Set<AccountingEntry>().AsQueryable();
            if (companyId.HasValue) query = query.Where(e => e.CompanyId == companyId.Value);
            var count = query.Count();
            return Task.FromResult(count);
        }
    }

    private sealed class LinkRepo(ZorvianDbContext db) : IAccountLinkRepository
    {
        public Task<AccountLink?> GetByTransactionTypeAndRoleAsync(string transactionType, string accountRole, Guid companyId) =>
            db.Set<AccountLink>().FirstOrDefaultAsync(al => al.TransactionType == transactionType && al.Role == accountRole && al.CompanyId == companyId);
        public Task<List<AccountLink>> GetByCompanyAsync(Guid companyId) => throw new NotImplementedException();
        public Task<List<AccountLink>> GetByTransactionTypeAsync(string transactionType, Guid companyId) => throw new NotImplementedException();
        public Task AddAsync(AccountLink link) => throw new NotImplementedException();
        public Task UpdateAsync(AccountLink link) => throw new NotImplementedException();
        public Task DeleteAsync(AccountLink link) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class AccRepo(ZorvianDbContext db) : IAccountRepository
    {
        public Task<Account?> GetByCodeAsync(string code, Guid companyId) =>
            db.Set<Account>().FirstOrDefaultAsync(a => a.Code == code && a.CompanyId == companyId);
        public Task<Account?> GetByIdAsync(Guid id) => db.Set<Account>().FindAsync(id).AsTask();
        public Task<List<Account>> GetAllAsync(Guid companyId) => throw new NotImplementedException();
        public Task<List<Account>> GetByTypeAsync(string type, Guid companyId) => throw new NotImplementedException();
        public Task<List<Account>> GetByParentAsync(Guid parentId) => throw new NotImplementedException();
        public Task<List<Account>> GetByCodesAsync(string[] codes, Guid companyId) => throw new NotImplementedException();
        public Task<List<Account>> GetActiveAsync(Guid companyId) => throw new NotImplementedException();
        public Task<bool> CodeExistsAsync(string code, Guid companyId) => throw new NotImplementedException();
        public Task<int> GetMaxLevelAsync(Guid? parentId, Guid companyId) => throw new NotImplementedException();
        public Task<bool> HasChildrenAsync(Guid id) => db.Set<Account>().AnyAsync(a => a.ParentId == id);
        public Task AddAsync(Account account) => throw new NotImplementedException();
        public Task UpdateAsync(Account account) => throw new NotImplementedException();
        public Task DeleteAsync(Account account) { db.Set<Account>().Remove(account); return Task.CompletedTask; }
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class SaleRepo(ZorvianDbContext db) : ISaleRepository
    {
        public async Task AddAsync(Sale sale) { await db.Set<Sale>().AddAsync(sale); await db.SaveChangesAsync(); }
        public Task<string> GenerateInvoiceNumberAsync(Guid companyId) => Task.FromResult($"FAC-{DateTime.UtcNow:yyyyMMdd}-0001");
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task<Sale?> GetByIdAsync(Guid id) => db.Set<Sale>().Include(s => s.Details).FirstOrDefaultAsync(s => s.Id == id);
        public Task UpdateAsync(Sale sale) { db.Set<Sale>().Update(sale); return Task.CompletedTask; }
        public Task<List<Sale>> GetFilteredAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(Guid? clientId, string? saleType, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId) => throw new NotImplementedException();
        public Task<decimal> GetTodaySalesAsync(Guid branchId) => throw new NotImplementedException();
        public Task<decimal> GetMonthSalesAsync(Guid branchId) => throw new NotImplementedException();
        public Task<decimal> GetAverageTicketAsync(Guid branchId) => throw new NotImplementedException();
        public Task<int> GetTodaySalesCountAsync(Guid branchId) => throw new NotImplementedException();
        public Task<List<SalesTrendMetrics>> GetSalesTrendRawAsync(DateTime from, DateTime to, Guid branchId, string currency) => Task.FromResult(new List<SalesTrendMetrics>());
        public Task<ExecutiveSalesMetrics> GetExecutiveSalesMetricsRawAsync(DateTime lastMonthStart, DateTime monthStart, DateTime todayStart, DateTime yesterdayStart, DateTime weekStart, DateTime weekEndExclusive, Guid branchId, string currency) =>
            Task.FromResult(new ExecutiveSalesMetrics(0, 0, 0, 0, 0, 0, 0, new List<decimal>()));
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitTransactionAsync() => db.SaveChangesAsync();
        public Task RollbackTransactionAsync() => Task.CompletedTask;
    }

    private sealed class ProductRepo(ZorvianDbContext db) : IProductRepository
    {
        public Task<Product?> GetByIdAsync(Guid id) => db.Set<Product>().Include(p => p.TaxCategory).FirstOrDefaultAsync(p => p.Id == id);
        public Task UpdateAsync(Product product) { db.Set<Product>().Update(product); return Task.CompletedTask; }
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task AddAsync(Product product) => throw new NotImplementedException();
        public Task<Product?> GetByCodeAsync(string code, Guid branchId) => throw new NotImplementedException();
        public Task<List<Product>> GetFilteredAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(string? search, Guid? categoryId, Guid? brandId, bool? lowStock, bool? isActive, Guid? branchId) => throw new NotImplementedException();
        public Task<List<Product>> GetOutOfStockAsync(Guid? branchId) => throw new NotImplementedException();
        public Task<List<Product>> GetLowStockAsync(Guid? branchId) => throw new NotImplementedException();
        public Task<int> GetTotalCountAsync(Guid? branchId) => throw new NotImplementedException();
        public Task<List<(Product Product, int TotalSold)>> GetTopSellingAsync(Guid? branchId, int count) => throw new NotImplementedException();
        public Task<InventorySummaryRaw> GetInventorySummaryRawAsync(Guid? branchId) => Task.FromResult(new InventorySummaryRaw(0, 0, 0, 0, 0, new List<InventoryCategoryRaw>(), new List<InventorySlowMoverRaw>()));
        public Task DeleteAsync(Product product) => throw new NotImplementedException();
        public Task<Product?> GetByIdForUpdateAsync(Guid id) => GetByIdAsync(id);
    }

    private sealed class MovementRepo(ZorvianDbContext db) : IInventoryMovementRepository
    {
        public Task AddAsync(InventoryMovement movement) { db.Set<InventoryMovement>().Add(movement); return Task.CompletedTask; }
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task<InventoryMovement?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<List<InventoryMovement>> GetFilteredAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(Guid? productId, string? movementType, DateTime? fromDate, DateTime? toDate, string? search, Guid? branchId) => throw new NotImplementedException();
    }

    private sealed class ClientRepo(ZorvianDbContext db) : IClientRepository
    {
        public Task<Client?> GetByIdAsync(Guid id) => db.Set<Client>().FirstOrDefaultAsync(c => c.Id == id);
        public Task<Client?> GetByCodeAsync(string code, Guid branchId) => throw new NotImplementedException();
        public Task<List<Client>> GetFilteredAsync(string? search, string? status, Guid branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(string? search, string? status, Guid branchId) => throw new NotImplementedException();
        public Task<string> GenerateCodeAsync(Guid companyId) => throw new NotImplementedException();
        public Task AddAsync(Client client) => throw new NotImplementedException();
        public Task UpdateAsync(Client client) => throw new NotImplementedException();
        public Task DeleteAsync(Client client) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class SupplierRepo(ZorvianDbContext db) : ISupplierRepository
    {
        public Task<Supplier?> GetByIdAsync(Guid id) => db.Set<Supplier>().FirstOrDefaultAsync(s => s.Id == id);
        public Task<List<Supplier>> GetAllAsync(Guid companyId) => throw new NotImplementedException();
        public Task<Supplier?> GetByTaxIdAsync(string taxId, Guid companyId) => throw new NotImplementedException();
        public Task<List<Supplier>> GetFilteredAsync(string? search, Guid companyId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(string? search, Guid companyId) => throw new NotImplementedException();
        public Task<string> GenerateCodeAsync(Guid companyId) => throw new NotImplementedException();
        public Task AddAsync(Supplier supplier) => throw new NotImplementedException();
        public Task UpdateAsync(Supplier supplier) => throw new NotImplementedException();
        public Task DeleteAsync(Supplier supplier) => throw new NotImplementedException();
        public Task SaveChangesAsync() => throw new NotImplementedException();
    }

    private sealed class PurchaseRepo(ZorvianDbContext db) : IPurchaseRepository
    {
        public async Task AddAsync(Purchase purchase) { await db.Set<Purchase>().AddAsync(purchase); await db.SaveChangesAsync(); }
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task<Purchase?> GetByIdAsync(Guid id) => db.Set<Purchase>().Include(p => p.Details).ThenInclude(d => d.Product).Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id);
        public Task UpdateAsync(Purchase purchase) { db.Set<Purchase>().Update(purchase); return Task.CompletedTask; }
        public Task<string> GeneratePurchaseNumberAsync(Guid companyId) => Task.FromResult($"COMP-{DateTime.UtcNow:yyyyMMdd}-0001");
        public Task<List<Purchase>> GetFilteredAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(Guid? supplierId, string? status, DateTime? fromDate, DateTime? toDate, string? search, Guid branchId) => throw new NotImplementedException();
        public Task<List<Purchase>> GetPendingAsync(Guid branchId) => throw new NotImplementedException();
        public Task<decimal> GetTotalPayableAsync(Guid companyId) => throw new NotImplementedException();
    }

    private sealed class CashMovementRepo(ZorvianDbContext db) : ICashMovementRepository
    {
        public Task<CashMovement?> GetByIdAsync(Guid id) => db.Set<CashMovement>().FirstOrDefaultAsync(m => m.Id == id);
        public Task<List<CashMovement>> GetByCashRegisterIdAsync(Guid cashRegisterId) => throw new NotImplementedException();
        public Task AddAsync(CashMovement movement) { db.Set<CashMovement>().Add(movement); return Task.CompletedTask; }
        public Task UpdateAsync(CashMovement movement) { db.Set<CashMovement>().Update(movement); return Task.CompletedTask; }
        public Task SaveChangesAsync() => db.SaveChangesAsync();
        public Task<List<CashMovement>> GetFilteredAsync(Guid? cashRegisterId, string? status, string? approvalStatus, DateTime? fromDate, DateTime? toDate, Guid branchId, int page, int pageSize) => throw new NotImplementedException();
        public Task<int> GetFilteredCountAsync(Guid? cashRegisterId, string? status, string? approvalStatus, DateTime? fromDate, DateTime? toDate, Guid branchId) => throw new NotImplementedException();
    }
}
