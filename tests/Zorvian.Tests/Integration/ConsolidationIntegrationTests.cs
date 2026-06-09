using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Services;
using Zorvian.Tests.Integration;

namespace Zorvian.Tests.Integration;

public class ConsolidationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConsolidationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPendingIntercompanyTransactionsAsync_ReturnsOnlyPendingTransactionsForCompany()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ZorvianDbContext>();
        var consolidationService = new ConsolidationService(dbContext);

        var companyA = new Company { Id = Guid.NewGuid(), Name = "Company A", TenantId = Guid.NewGuid().ToString() };
        var companyB = new Company { Id = Guid.NewGuid(), Name = "Company B", TenantId = Guid.NewGuid().ToString() };
        dbContext.Companies.AddRange(companyA, companyB);

        var transactionPending = new IntercompanyTransaction 
        { 
            Id = Guid.NewGuid(), 
            FromCompanyId = companyA.Id, 
            ToCompanyId = companyB.Id, 
            Amount = 1000, 
            Status = "Pending" 
        };
        var transactionCompleted = new IntercompanyTransaction 
        { 
            Id = Guid.NewGuid(), 
            FromCompanyId = companyA.Id, 
            ToCompanyId = companyB.Id, 
            Amount = 500, 
            Status = "Completed" 
        };
        dbContext.IntercompanyTransactions.AddRange(transactionPending, transactionCompleted);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await consolidationService.GetPendingIntercompanyTransactionsAsync(companyA.Id);

        // Assert
        Assert.Single(result);
        Assert.Equal(transactionPending.Id, result.First().Id);
    }
}
