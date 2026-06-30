using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Tests.Services;

public sealed class CountryTaxConfigSeederTests : IDisposable
{
    private readonly ZorvianDbContext _db;
    private readonly ILogger _logger = NullLogger.Instance;

    public CountryTaxConfigSeederTests()
    {
        var options = new DbContextOptionsBuilder<ZorvianDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantMock = new Mock<Core.Interfaces.ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _db = new ZorvianDbContext(options, tenantMock.Object);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SeedAsync_CreatesSixCountryConfigs()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var configs = await _db.CountryTaxConfigs.ToListAsync();

        Assert.Equal(6, configs.Count);
    }

    [Fact]
    public async Task SeedAsync_SkipsIfAlreadySeeded()
    {
        _db.CountryTaxConfigs.Add(new CountryTaxConfig
        {
            CountryCode = "NIC",
            CountryName = "Nicaragua",
            Currency = "NIO",
            IsActive = true,
        });
        await _db.SaveChangesAsync();

        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var configs = await _db.CountryTaxConfigs.ToListAsync();
        Assert.Single(configs);
    }

    [Fact]
    public async Task SeedAsync_CreatesNicaraguaConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var nic = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "NIC");

        Assert.NotNull(nic);
        Assert.Equal("Nicaragua", nic.CountryName);
        Assert.Equal("NIO", nic.Currency);
        Assert.Equal(0.07m, nic.InssEmployeeRate);
        Assert.Equal(0.225m, nic.InssEmployerRate);
        Assert.Equal(15, nic.VacationDaysPerYear);
        Assert.True(nic.HasThirteenthMonth);
        Assert.False(nic.HasFourteenthMonth);
        Assert.True(nic.IsActive);
    }

    [Fact]
    public async Task SeedAsync_CreatesCostaRicaConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var cr = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "CRI");

        Assert.NotNull(cr);
        Assert.Equal("Costa Rica", cr.CountryName);
        Assert.Equal("CRC", cr.Currency);
        Assert.Equal(0.1083m, cr.InssEmployeeRate);
        Assert.Equal(0.2683m, cr.InssEmployerRate);
        Assert.Equal(12, cr.VacationDaysPerYear);
        Assert.True(cr.HasThirteenthMonth);
        Assert.True(cr.HasFourteenthMonth);
    }

    [Fact]
    public async Task SeedAsync_CreatesPanamaConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var pan = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "PAN");

        Assert.NotNull(pan);
        Assert.Equal("Panamá", pan.CountryName);
        Assert.Equal("USD", pan.Currency);
        Assert.Equal(0.0975m, pan.InssEmployeeRate);
        Assert.Equal(0.1325m, pan.InssEmployerRate);
        Assert.Equal(30, pan.VacationDaysPerYear);
        Assert.Equal(0.25m, pan.ChristmasBonusPercentage);
    }

    [Fact]
    public async Task SeedAsync_CreatesAllSixCountryCodes()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var codes = await _db.CountryTaxConfigs.Select(c => c.CountryCode).ToListAsync();

        Assert.Contains("NIC", codes);
        Assert.Contains("CRI", codes);
        Assert.Contains("PAN", codes);
        Assert.Contains("HND", codes);
        Assert.Contains("SLV", codes);
        Assert.Contains("GTM", codes);
    }

    [Fact]
    public async Task SeedAsync_AllConfigsAreActive()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var allActive = await _db.CountryTaxConfigs.AllAsync(c => c.IsActive);
        Assert.True(allActive);
    }

    [Fact]
    public async Task SeedAsync_AllConfigsHaveIrTableJson()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var allHaveIr = await _db.CountryTaxConfigs.AllAsync(c => !string.IsNullOrEmpty(c.IrTableJson) && c.IrTableJson != "[]");
        Assert.True(allHaveIr);
    }

    [Fact]
    public async Task SeedAsync_HondurasConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var hnd = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "HND");

        Assert.NotNull(hnd);
        Assert.Equal("Honduras", hnd.CountryName);
        Assert.Equal("HNL", hnd.Currency);
        Assert.Equal(0.05m, hnd.InssEmployeeRate);
        Assert.Equal(0.125m, hnd.InssEmployerRate);
        Assert.Equal(12, hnd.VacationDaysPerYear);
    }

    [Fact]
    public async Task SeedAsync_ElSalvadorConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var slv = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "SLV");

        Assert.NotNull(slv);
        Assert.Equal("El Salvador", slv.CountryName);
        Assert.Equal("USD", slv.Currency);
        Assert.Equal(0.03m, slv.InssEmployeeRate);
        Assert.Equal(0.175m, slv.InssEmployerRate);
        Assert.Equal(15, slv.VacationDaysPerYear);
    }

    [Fact]
    public async Task SeedAsync_GuatemalaConfig()
    {
        await CountryTaxConfigSeeder.SeedAsync(_db, _logger);

        var gtm = await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == "GTM");

        Assert.NotNull(gtm);
        Assert.Equal("Guatemala", gtm.CountryName);
        Assert.Equal("GTQ", gtm.Currency);
        Assert.Equal(0.0483m, gtm.InssEmployeeRate);
        Assert.Equal(0.1267m, gtm.InssEmployerRate);
        Assert.Equal(15, gtm.VacationDaysPerYear);
    }
}
