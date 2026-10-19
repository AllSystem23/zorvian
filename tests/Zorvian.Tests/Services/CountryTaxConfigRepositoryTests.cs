using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;

namespace Zorvian.Tests.Services;

/// <summary>
/// Resolución de la configuración fiscal por país: la compañía puede tener su propio
/// ajuste (fila con SU tenant) sobre el catálogo global (tenant centinela). El ajuste
/// gana; si no existe, se usa el catálogo; las filas de OTRAS compañías nunca se ven.
/// </summary>
public sealed class CountryTaxConfigRepositoryTests
{
    private readonly Mock<ITenantContext> _tenant = new();
    private readonly ZorvianDbContext _db;
    private readonly CountryTaxConfigRepository _sut;
    private readonly string _tenantA = Guid.NewGuid().ToString();
    private readonly string _tenantB = Guid.NewGuid().ToString();

    public CountryTaxConfigRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _tenant.Setup(t => t.TenantId).Returns(_tenantA);
        _db = new ZorvianDbContext(options, _tenant.Object);
        _sut = new CountryTaxConfigRepository(_db, _tenant.Object);

        Seed("NIC", CountryTaxConfig.GlobalTenantId, 0.07m);
        Seed("GTM", CountryTaxConfig.GlobalTenantId, 0.0483m);
        Seed("NIC", _tenantB, 0.05m); // ajuste de OTRA compañía
    }

    private void Seed(string countryCode, string tenantId, decimal employeeRate)
    {
        _db.CountryTaxConfigs.Add(new CountryTaxConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CompanyId = Guid.TryParse(tenantId, out var companyId) ? companyId : Guid.Empty,
            CountryCode = countryCode,
            CountryName = countryCode,
            Currency = "NIO",
            InssEmployeeRate = employeeRate,
            IrTableJson = "[]",
            IsActive = true,
        });
        _db.SaveChanges();
    }

    private void UseTenant(string tenantId)
    {
        _tenant.Setup(t => t.TenantId).Returns(TenantId.FromString(tenantId));
    }

    [Fact]
    public async Task GetByCountryCodeAsync_PrefersOwnOverride()
    {
        Seed("NIC", _tenantA, 0.08m);

        var result = await _sut.GetByCountryCodeAsync("NIC");

        result.Should().NotBeNull();
        result!.TenantId.Should().Be(_tenantA);
        result.InssEmployeeRate.Should().Be(0.08m);
    }

    [Fact]
    public async Task GetByCountryCodeAsync_WithoutOverride_FallsBackToGlobal()
    {
        var result = await _sut.GetByCountryCodeAsync("NIC");

        result.Should().NotBeNull();
        result!.TenantId.Should().Be(CountryTaxConfig.GlobalTenantId);
        result.InssEmployeeRate.Should().Be(0.07m);
    }

    [Fact]
    public async Task GetByCountryCodeAsync_IgnoresAnotherCompanyOverride()
    {
        var result = await _sut.GetByCountryCodeAsync("NIC");

        result!.TenantId.Should().Be(CountryTaxConfig.GlobalTenantId);
    }

    [Fact]
    public async Task GetByCountryCodeAsync_WithoutCompanyScope_ReturnsGlobal()
    {
        UseTenant(CountryTaxConfig.GlobalTenantId);
        Seed("NIC", _tenantA, 0.08m);

        var result = await _sut.GetByCountryCodeAsync("NIC");

        result!.TenantId.Should().Be(CountryTaxConfig.GlobalTenantId);
        result.InssEmployeeRate.Should().Be(0.07m);
    }

    [Fact]
    public async Task GetByCountryCodeAsync_IsCaseAndWhitespaceInsensitive()
    {
        var result = await _sut.GetByCountryCodeAsync("  nic ");

        result.Should().NotBeNull();
        result!.CountryCode.Should().Be("NIC");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyGlobalCatalog()
    {
        Seed("NIC", _tenantA, 0.08m);

        var items = await _sut.GetAllAsync();

        items.Should().HaveCount(2);
        items.Should().OnlyContain(c => c.TenantId == CountryTaxConfig.GlobalTenantId);
    }

    [Fact]
    public async Task GetByIdAsync_IgnoresCompanyRows()
    {
        var own = new CountryTaxConfig
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantA,
            CountryCode = "HND",
            CountryName = "Honduras",
            IsActive = true,
        };
        _db.CountryTaxConfigs.Add(own);
        _db.SaveChanges();

        var result = await _sut.GetByIdAsync(own.Id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsByCountryCodeAsync_CompanyOverride_DoesNotBlockGlobalCatalog()
    {
        Seed("HND", _tenantA, 0.025m);

        var exists = await _sut.ExistsByCountryCodeAsync("HND");

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetTenantOverrideAsync_ReturnsOnlyThatTenantRow()
    {
        var own = await _sut.GetTenantOverrideAsync("NIC", _tenantA);
        var other = await _sut.GetTenantOverrideAsync("NIC", _tenantB);

        own.Should().BeNull();
        other.Should().NotBeNull();
        other!.InssEmployeeRate.Should().Be(0.05m);
    }
}
