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
    private readonly Mock<ITenantContextWriter> _tenantWriterMock = new();
    private readonly ZorvianDbContext _db;

    public PalmtrackFeatureFlagsControllerTests()
    {
        _tenantId = new TenantId(Guid.NewGuid());
        _tenantMock.Setup(t => t.TenantId).Returns(_tenantId);
        _tenantMock.Setup(t => t.IsSuperAdmin).Returns(false);
        _tenantMock.Setup(t => t.SelectedCompanyId).Returns(_tenantId.Value);
        _tenantMock.Setup(t => t.HasCompanySelection).Returns(true);
        _tenantMock.Setup(t => t.BypassTenantFilter).Returns(false);

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
        new(new CompanyRepository(_db, _tenantMock.Object), _tenantMock.Object, _tenantWriterMock.Object, BuildConfig(ssoEnabledInConfig));

    /// <summary>
    /// Simula una sesión sin empresa seleccionada (TenantId vacío), que es
    /// el estado típico de un SuperAdmin antes de elegir empresa.
    /// </summary>
    private void ConfigureWithoutTenantSelected(bool isSuperAdmin)
    {
        _tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.Empty));
        _tenantMock.Setup(t => t.IsSuperAdmin).Returns(isSuperAdmin);
        _tenantMock.Setup(t => t.SelectedCompanyId).Returns((Guid?)null);
        _tenantMock.Setup(t => t.HasCompanySelection).Returns(false);
        // Sólo el SuperAdmin sin compañía seleccionada bypasea el filtro de tenant (semántica de ITenantContext.BypassTenantFilter).
        _tenantMock.Setup(t => t.BypassTenantFilter).Returns(isSuperAdmin);
    }

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

    [Fact]
    public async Task Put_WithoutTenantAndNotSuperAdmin_Returns401()
    {
        await SeedCompanyAsync();
        ConfigureWithoutTenantSelected(isSuperAdmin: false);
        var sut = CreateSut();

        var result = await sut.UpdateFeatureFlags(AllTrue());

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Put_AsSuperAdminWithoutTenant_SelectsFirstActiveCompanyAndPersists()
    {
        var company = await SeedCompanyAsync();
        ConfigureWithoutTenantSelected(isSuperAdmin: true);
        var sut = CreateSut();

        var putResult = await sut.UpdateFeatureFlags(AllTrue());
        ReadOk(putResult).Should().AllSatisfy(kv => kv.Value.Should().BeTrue());

        // Se auto-selecciona el tenant de la empresa resuelta para el resto del request
        _tenantWriterMock.Verify(w => w.SetTenantId(It.Is<TenantId>(t => t.Value == Guid.Parse(company.TenantId))), Times.Once);

        // Y el flag queda persistido en CompanySettings de esa empresa
        var row = await _db.CompanySettings.SingleAsync();
        row.CompanyId.Should().Be(company.Id);
        row.PalmTrackEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Put_AsSuperAdminWithoutTenantAndNoCompanies_CreatesDefaultCompanyAndPersists()
    {
        ConfigureWithoutTenantSelected(isSuperAdmin: true);
        var sut = CreateSut();

        var putResult = await sut.UpdateFeatureFlags(AllTrue());
        ReadOk(putResult).Should().AllSatisfy(kv => kv.Value.Should().BeTrue());

        var created = await _db.Companies.SingleAsync();
        created.IsActive.Should().BeTrue();
        _tenantWriterMock.Verify(w => w.SetTenantId(It.Is<TenantId>(t => t.Value == Guid.Parse(created.TenantId))), Times.Once);

        var row = await _db.CompanySettings.SingleAsync();
        row.CompanyId.Should().Be(created.Id);
        row.PalmTrackEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Get_AsSuperAdminWithoutTenant_ReturnsSettingsOfFirstActiveCompany()
    {
        var company = await SeedCompanyAsync();
        await _db.CompanySettings.AddAsync(new CompanySettings
        {
            CompanyId = company.Id,
            TenantId = company.TenantId,
            PalmTrackEnabled = true,
            PalmTrackSsoEnabled = true,
        });
        await _db.SaveChangesAsync();

        ConfigureWithoutTenantSelected(isSuperAdmin: true);
        var sut = CreateSut();

        var flags = ReadOk(await sut.GetFeatureFlags());

        // Sin tenant seleccionado, el SuperAdmin ve lo persistido (no el fallback de appsettings)
        flags["moduleEnabled"].Should().BeTrue();
        flags["ssoEnabled"].Should().BeTrue();
        flags["ssoAutoCreateUsers"].Should().BeFalse();
    }

    [Fact]
    public async Task Get_WithoutTenantAndNotSuperAdmin_FallsBackToAppSettings()
    {
        await SeedCompanyAsync();
        ConfigureWithoutTenantSelected(isSuperAdmin: false);
        var sut = CreateSut(ssoEnabledInConfig: true);

        var flags = ReadOk(await sut.GetFeatureFlags());

        flags["moduleEnabled"].Should().BeFalse();
        flags["ssoEnabled"].Should().BeTrue();
    }
}