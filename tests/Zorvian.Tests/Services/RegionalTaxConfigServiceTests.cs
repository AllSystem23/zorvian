using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Core.Interfaces;

namespace Zorvian.Tests.Services;

public sealed class RegionalTaxConfigServiceTests
{
    private readonly ZorvianDbContext _db;
    private readonly RegionalTaxConfigurationRepository _repo;
    private readonly RegionalTaxConfigService _sut;
    private readonly Guid _tenantId;

    public RegionalTaxConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenantId = Guid.NewGuid();
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(_tenantId));
        _db = new ZorvianDbContext(options, tenantMock.Object);

        _repo = new RegionalTaxConfigurationRepository(_db);
        _sut = new RegionalTaxConfigService(_repo);
    }

    [Fact]
    public async Task GetActiveTaxesAsync_Should_Return_Only_Active_Taxes()
    {
        var companyId = Guid.NewGuid();
        var country = "NIC";
        var tenantIdStr = _tenantId.ToString();

        var tax1 = new Zorvian.Core.Entities.RegionalTaxConfiguration 
            { CountryCode = country, TaxType = "IVA", Rate = 0.15m, CompanyId = companyId, IsActive = true, TenantId = tenantIdStr };
        var tax2 = new Zorvian.Core.Entities.RegionalTaxConfiguration 
            { CountryCode = country, TaxType = "IR", Rate = 0.02m, CompanyId = companyId, IsActive = false, TenantId = tenantIdStr };

        _db.RegionalTaxConfigurations.AddRange(tax1, tax2);
        await _db.SaveChangesAsync();

        var result = await _sut.GetActiveTaxesAsync(country, companyId);


        Assert.Single(result);
        Assert.Equal("IVA", result.First().TaxType);
    }
}
