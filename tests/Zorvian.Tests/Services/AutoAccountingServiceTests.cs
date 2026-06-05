using Moq;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
}
