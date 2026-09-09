using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Infrastructure.Repositories;
using Zorvian.Web.Controllers.Settings;

namespace Zorvian.Tests.Controllers;

/// <summary>
/// Tests para PalmtrackFeatureFlagsController: verifica que los feature flags
/// se persistan en CompanySettings y que GET devuelva los valores guardados
/// (comportamiento "guardar → recargar").
/// </summary>
public sealed class PalmtrackFeatureFlagsControllerTests : IDisposable
{
    private readonly TenantId _tenantId;
    private readonly Mock<ITenantContext> _tenantMock = new();
    private readonly ZorvianDbContext _db;

    public PalmtrackFeatureFlagsControllerTests()
    {
        _tenantId = new TenantId(Guid.NewGuid());
        _tenantMock.Setup(t => t.TenantId).Returns(_tenantId);
        _tenantMock.Setup(t => t.IsSuperAdmin).Returns(false);

        _db = new ZorvianDbContext(
            new DbContextOptionsBuilder<ZorvianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options,
            _tenantMock.Object);
    }

    public void Dispose() => _db.Dispose();

    private static IConfiguration BuildConfig(bool ssoEnabled = false) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PalmTrack:Enabled"] = "false",
                ["PalmTrack:SsoEnabled"] = ssoEnabled ? "true" : "false",
                ["PalmTrack:SsoAutoCreateUsers"] = "false",
                ["PalmTrack:SsoPropagateRoles"] = "false",
                ["PalmTrack:SsoSharedProject"] = "false",
            })
            .Build();

    private PalmtrackFeatureFlagsController CreateSut(bool ssoEnabledInConfig = false) =>
        new(new CompanyRepository(_db), _tenantMock.Object, BuildConfig(ssoEnabledInConfig));

    private async Task<Company> SeedCompanyAsync()
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId.ToString(),
            Name = "TestCo",
            LegalName = "TestCo S.A.",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
        };
        _db.Companies.Add(company);
        await _db.SaveChangesAsync();
        return company;
    }

    private static Dictionary<string, bool> ReadOk(IActionResult result)
    {
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        return JsonSerializer.Deserialize<Dictionary<string, bool>>(json)!;
    }

    private static UpdateFeatureFlagsRequest AllTrue() =>
        new(true, true, true, true, true);

    [Fact]
    public async Task Get_WithoutSettingsRow_FallsBackToAppSettingsConfig()
    {
        await SeedCompanyAsync();
        var sut = CreateSut(ssoEnabledInConfig: true);

        var result = await sut.GetFeatureFlags();

        var flags = ReadOk(result);
        flags["moduleEnabled"].Should().BeFalse();
        flags["ssoEnabled"].Should().BeTrue();
        flags["ssoAutoCreateUsers"].Should().BeFalse();
        flags["ssoPropagateRoles"].Should().BeFalse();
        flags["ssoSharedProject"].Should().BeFalse();
    }

    [Fact]
    public async Task Put_ThenGet_WithNewSettingsRow_PersistsValuesAcrossReloads()
    {
        await SeedCompanyAsync();
        var sut = CreateSut();

        // Save via PUT
        var putResult = await sut.UpdateFeatureFlags(AllTrue());
        ReadOk(putResult).Should().AllSatisfy(kv => kv.Value.Should().BeTrue());

        // Simulate page reload: a fresh controller reading from the same DB
        var reloadedSut = CreateSut();
        var getResult = await reloadedSut.GetFeatureFlags();

        var flags = ReadOk(getResult);
        flags["moduleEnabled"].Should().BeTrue();
        flags["ssoEnabled"].Should().BeTrue();
        flags["ssoAutoCreateUsers"].Should().BeTrue();
        flags["ssoPropagateRoles"].Should().BeTrue();
        flags["ssoSharedProject"].Should().BeTrue();
    }

    [Fact]
    public async Task Put_WithExistingSettingsRow_UpdatesOnlyProvidedFlags()
    {
        var company = await SeedCompanyAsync();
        await _db.CompanySettings.AddAsync(new CompanySettings
        {
            CompanyId = company.Id,
            TenantId = _tenantId.ToString(),
            PalmTrackEnabled = true,
            PalmTrackSsoEnabled = false,
        });
        await _db.SaveChangesAsync();

        var sut = CreateSut();

        // Turn OFF the module and leave SSO flags untouched
        var putResult = await sut.UpdateFeatureFlags(new UpdateFeatureFlagsRequest(
            ModuleEnabled: false,
            SsoEnabled: null,
            SsoAutoCreateUsers: null,
            SsoPropagateRoles: null,
            SsoSharedProject: null));

        var saved = ReadOk(putResult);
        saved["moduleEnabled"].Should().BeFalse();
        saved["ssoEnabled"].Should().BeFalse();

        var row = await _db.CompanySettings.SingleAsync();
        row.PalmTrackEnabled.Should().BeFalse();
        row.PalmTrackSsoEnabled.Should().BeFalse();
        row.PalmTrackSsoSharedProject.Should().BeFalse();
    }

    [Fact]
    public async Task Put_WithoutCompany_Returns404()
    {
        var sut = CreateSut();

        var result = await sut.UpdateFeatureFlags(AllTrue());

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}