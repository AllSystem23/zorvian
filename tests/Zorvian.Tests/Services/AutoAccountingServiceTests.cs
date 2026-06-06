using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Zorvian.Tests.Services;

public class AutoAccountingServiceTests
{
    [Fact]
    public async Task GenerateCashMovementEntryAsync_ShouldCreateCorrectEntry()
    {
        // ARRANGE
        var movementId = Guid.NewGuid();
        var mockRepo = new Mock<ICashMovementRepository>();
        var entryRepo = new Mock<IAccountingEntryRepository>();
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        var linkRepo = new Mock<IAccountLinkRepository>();
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var tenant = new Mock<ITenantContext>();
        var payrollRepo = new Mock<IPayrollRepository>();

        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: "AutoAccountingTestsDb_" + Guid.NewGuid())
            .Options;
        var db = new ZorvianDbContext(options, tenant.Object);

        var tenantId = Guid.NewGuid();
        var movement = new CashMovement { Id = movementId, Amount = 100m, MovementType = "Income", Concept = "Test", TenantId = tenantId.ToString() };
        mockRepo.Setup(r => r.GetByIdAsync(movementId)).ReturnsAsync(movement);
        tenant.Setup(t => t.TenantId).Returns(tenantId.ToString());
        
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new AccountLink { AccountId = Guid.NewGuid() });

        var sut = new AutoAccountingService(
            entryRepo.Object, periodRepo.Object, linkRepo.Object, ruleRepo.Object, 
            accountRepo.Object, tenant.Object, payrollRepo.Object, mockRepo.Object);

        entryRepo.Setup(r => r.AddAsync(It.IsAny<AccountingEntry>()))
            .Callback<AccountingEntry>(e => {
                if (e.Id == Guid.Empty) e.Id = Guid.NewGuid();
            });

        // ACT
        var result = await sut.GenerateCashMovementEntryAsync(movementId);
        Assert.NotEqual(Guid.Empty, result);
        entryRepo.Verify(r => r.AddAsync(It.IsAny<AccountingEntry>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCreditPaymentEntryAsync_ShouldCreateCorrectEntry()
    {
        // ARRANGE
        var paymentId = Guid.NewGuid();
        var creditId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        
        var entryRepo = new Mock<IAccountingEntryRepository>();
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        var linkRepo = new Mock<IAccountLinkRepository>();
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var tenant = new Mock<ITenantContext>();
        var payrollRepo = new Mock<IPayrollRepository>();
        var cashRepo = new Mock<ICashMovementRepository>();

        tenant.Setup(t => t.TenantId).Returns(companyId.ToString());
        
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new AccountLink { AccountId = Guid.NewGuid() });

        var sut = new AutoAccountingService(
            entryRepo.Object, periodRepo.Object, linkRepo.Object, ruleRepo.Object, 
            accountRepo.Object, tenant.Object, payrollRepo.Object, cashRepo.Object);

        // ACT
        var result = await sut.GenerateCreditPaymentEntryAsync(paymentId, creditId, 80m, 20m, companyId, branchId);

        // ASSERT
        Assert.NotEqual(Guid.Empty, result);
        entryRepo.Verify(r => r.AddAsync(It.Is<AccountingEntry>(e => 
            e.TotalDebit == 100m && 
            e.TotalCredit == 100m && 
            e.ReferenceId == paymentId)), Times.Once);
        entryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateWarrantyCostEntryAsync_ShouldCreateCorrectEntry_WhenCompanyPays()
    {
        var costId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var entryRepo = new Mock<IAccountingEntryRepository>();
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        var linkRepo = new Mock<IAccountLinkRepository>();
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var tenant = new Mock<ITenantContext>();
        var payrollRepo = new Mock<IPayrollRepository>();
        var cashRepo = new Mock<ICashMovementRepository>();

        tenant.Setup(t => t.TenantId).Returns(companyId.ToString());

        var expenseAccountId = Guid.NewGuid();
        var cashAccountId = Guid.NewGuid();

        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.WarrantyLaborExpense, companyId))
            .ReturnsAsync(new AccountLink { AccountId = expenseAccountId });
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.Cash, companyId))
            .ReturnsAsync(new AccountLink { AccountId = cashAccountId });

        var sut = new AutoAccountingService(
            entryRepo.Object, periodRepo.Object, linkRepo.Object, ruleRepo.Object,
            accountRepo.Object, tenant.Object, payrollRepo.Object, cashRepo.Object);

        AccountingEntry? captured = null;
        entryRepo.Setup(r => r.AddAsync(It.IsAny<AccountingEntry>()))
            .Callback<AccountingEntry>(e => { captured = e; if (e.Id == Guid.Empty) e.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        var result = await sut.GenerateWarrantyCostEntryAsync(costId, "labor", 100m, "company", null, warrantyId, companyId, branchId);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(captured);
        Assert.Equal(100m, captured.TotalDebit);
        Assert.Equal(100m, captured.TotalCredit);
        Assert.Equal(costId, captured.ReferenceId);
        Assert.Equal("WarrantyCost", captured.ReferenceType);
        Assert.Equal(2, captured.Details.Count);
        Assert.Contains(captured.Details, d => d.AccountId == expenseAccountId && d.DebitAmount == 100m);
        Assert.Contains(captured.Details, d => d.AccountId == cashAccountId && d.CreditAmount == 100m);
        entryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateWarrantyCostEntryAsync_ShouldCreateCorrectEntry_WhenProviderPays()
    {
        var costId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var entryRepo = new Mock<IAccountingEntryRepository>();
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        var linkRepo = new Mock<IAccountLinkRepository>();
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var tenant = new Mock<ITenantContext>();
        var payrollRepo = new Mock<IPayrollRepository>();
        var cashRepo = new Mock<ICashMovementRepository>();

        tenant.Setup(t => t.TenantId).Returns(companyId.ToString());

        var expenseAccountId = Guid.NewGuid();
        var receivableAccountId = Guid.NewGuid();

        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.WarrantyPartsExpense, companyId))
            .ReturnsAsync(new AccountLink { AccountId = expenseAccountId });
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.WarrantyProviderReceivable, companyId))
            .ReturnsAsync(new AccountLink { AccountId = receivableAccountId });

        var sut = new AutoAccountingService(
            entryRepo.Object, periodRepo.Object, linkRepo.Object, ruleRepo.Object,
            accountRepo.Object, tenant.Object, payrollRepo.Object, cashRepo.Object);

        AccountingEntry? captured = null;
        entryRepo.Setup(r => r.AddAsync(It.IsAny<AccountingEntry>()))
            .Callback<AccountingEntry>(e => { captured = e; if (e.Id == Guid.Empty) e.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        var result = await sut.GenerateWarrantyCostEntryAsync(costId, "parts", 250m, "provider", providerId, warrantyId, companyId, branchId);

        Assert.NotEqual(Guid.Empty, result);
        Assert.NotNull(captured);
        Assert.Equal(250m, captured.TotalDebit);
        Assert.Equal(250m, captured.TotalCredit);
        Assert.Contains(captured.Details, d => d.AccountId == expenseAccountId && d.DebitAmount == 250m);
        Assert.Contains(captured.Details, d => d.AccountId == receivableAccountId && d.CreditAmount == 250m);
        entryRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GenerateWarrantyCostEntryAsync_ShouldUseDefaultExpenseRole_WhenCategoryNotMapped()
    {
        var costId = Guid.NewGuid();
        var warrantyId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var entryRepo = new Mock<IAccountingEntryRepository>();
        var periodRepo = new Mock<IAccountingPeriodRepository>();
        var linkRepo = new Mock<IAccountLinkRepository>();
        var ruleRepo = new Mock<IAccountingRuleRepository>();
        var accountRepo = new Mock<IAccountRepository>();
        var tenant = new Mock<ITenantContext>();
        var payrollRepo = new Mock<IPayrollRepository>();
        var cashRepo = new Mock<ICashMovementRepository>();

        tenant.Setup(t => t.TenantId).Returns(companyId.ToString());

        var expenseAccountId = Guid.NewGuid();
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.WarrantyExpense, companyId))
            .ReturnsAsync(new AccountLink { AccountId = expenseAccountId });
        linkRepo.Setup(r => r.GetByTransactionTypeAndRoleAsync(TransactionTypes.WarrantyCost, AccountRoles.Cash, companyId))
            .ReturnsAsync(new AccountLink { AccountId = Guid.NewGuid() });

        var sut = new AutoAccountingService(
            entryRepo.Object, periodRepo.Object, linkRepo.Object, ruleRepo.Object,
            accountRepo.Object, tenant.Object, payrollRepo.Object, cashRepo.Object);

        AccountingEntry? captured = null;
        entryRepo.Setup(r => r.AddAsync(It.IsAny<AccountingEntry>()))
            .Callback<AccountingEntry>(e => { captured = e; if (e.Id == Guid.Empty) e.Id = Guid.NewGuid(); })
            .Returns(Task.CompletedTask);

        await sut.GenerateWarrantyCostEntryAsync(costId, "fees", 50m, "company", null, warrantyId, companyId, branchId);

        Assert.NotNull(captured);
        Assert.Contains(captured.Details, d => d.AccountId == expenseAccountId);
    }
}
