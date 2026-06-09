using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zorvian.Application.DTOs.Approval;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Tests.Integration;

namespace Zorvian.Tests.Integration;

public class TreasuryIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TreasuryIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IssueCheck_PersistsCheckAndAuditTrailAndTriggersApproval()
    {
        // Arrange
        var mockApprovalEngine = new Mock<IApprovalEngine>();
        mockApprovalEngine
            .Setup(e => e.EvaluateAsync("Treasury", "CheckIssuance", It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .ReturnsAsync(new ApprovalEvaluationResult(true, Guid.NewGuid(), "Pending"));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        
        var checkRepo = scope.ServiceProvider.GetRequiredService<ICheckRepository>();
        var checkbookRepo = scope.ServiceProvider.GetRequiredService<ICheckbookRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<ICheckAuditTrailRepository>();
        var templateRepo = scope.ServiceProvider.GetRequiredService<ICheckPrintTemplateRepository>();
        
        var treasuryService = new TreasuryService(checkRepo, checkbookRepo, auditRepo, templateRepo, mockApprovalEngine.Object);
        
        var userId = Guid.NewGuid();

        var bank = new Bank { Id = Guid.NewGuid(), Name = "Bank Test" };
        dbContext.Banks.Add(bank);
        
        var bankAccount = new BankAccount { Id = Guid.NewGuid(), BankId = bank.Id, AccountNumber = "123" };
        dbContext.BankAccounts.Add(bankAccount);
        await dbContext.SaveChangesAsync();

        var check = new Check 
        { 
            Id = Guid.NewGuid(), 
            BankAccountId = bankAccount.Id, 
            Beneficiary = "Supplier Test", 
            Amount = 500,
            CheckNumber = 1001,
            IssueDate = DateTime.UtcNow
        };

        // Act
        await treasuryService.IssueCheckAsync(check, userId);

        // Assert
        var savedCheck = await dbContext.Checks.FindAsync(check.Id);
        Assert.NotNull(savedCheck);
        Assert.Equal(CheckStatus.PendingApproval, savedCheck!.Status);
        
        mockApprovalEngine.Verify(e => e.EvaluateAsync("Treasury", "CheckIssuance", check.Id, 500, userId.ToString()), Times.Once);

        var auditTrail = dbContext.CheckAuditTrails.FirstOrDefault(a => a.CheckId == check.Id);
        Assert.NotNull(auditTrail);
        Assert.Equal("Issued", auditTrail!.Action);
    }
}
