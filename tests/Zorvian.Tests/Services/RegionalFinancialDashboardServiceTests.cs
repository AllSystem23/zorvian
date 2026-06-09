using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Application.Services;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Core.Interfaces;
using Zorvian.Core.Models;
using Zorvian.Core.Entities;

namespace Zorvian.Tests.Services;

public sealed class RegionalFinancialDashboardServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly RegionalDashboardRepository _repo;
    private readonly RegionalFinancialDashboardService _sut;

    public RegionalFinancialDashboardServiceTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _db = new ZorvianDbContext(options, tenantMock.Object);
        
        _repo = new RegionalDashboardRepository(_db);
        _sut = new RegionalFinancialDashboardService(_repo);
    }

    [Fact]
    public async Task GetRegionalKpisAsync_Should_Return_Correct_Structure()
    {
        var companyId = Guid.NewGuid();
        var country = "NIC";
        _db.RegionalTaxConfigurations.Add(new Zorvian.Core.Entities.RegionalTaxConfiguration 
            { CountryCode = country, TaxType = "IVA", Rate = 0.15m, CompanyId = companyId, IsActive = true });
        await _db.SaveChangesAsync();

        var result = await _sut.GetRegionalKpisAsync(companyId, country);

        Assert.NotNull(result);
        // Using dynamic reflection to check properties since the return type is anonymous in the service
        var type = result.GetType();
        Assert.NotNull(type.GetProperty("Country"));
        Assert.NotNull(type.GetProperty("ActiveTaxes"));
        Assert.NotNull(type.GetProperty("PayrollSummary"));
    }
}
