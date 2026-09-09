using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Controllers.Fleet;

namespace Zorvian.Tests.Controllers;

/// <summary>
/// Tests para PalmTrackReadController bajo el modelo de módulo de PLATAFORMA:
///  - El acceso se controla con CompanySettings.PalmTrackEnabled (activación por empresa).
///  - El orgId se resuelve: mapping por tenant (si existe) → PalmTrack:DefaultOrgId.
///  - Super admins pueden pasar ?orgId= explícito; usuarios normales no.
/// </summary>
public sealed class PalmTrackReadControllerTests : IDisposable
{
    private readonly Mock<ILogger<PalmTrackReadController>> _logger = new();
    private readonly Mock<ITenantContext> _tenantMock = new();
    private readonly ZorvianDbContext _db;

    public PalmTrackReadControllerTests()
    {
        _tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _tenantMock.Setup(t => t.IsSuperAdmin).Returns(false);
        _db = new ZorvianDbContext(
            new DbContextOptionsBuilder<ZorvianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options,
            _tenantMock.Object);
    }

    public void Dispose() => _db.Dispose();

    /// <summary>
    /// Siembra una empresa con el módulo PalmTrack activado (o desactivado).
    /// Sin empresa, el controller hace fallback a la config PalmTrack:Enabled.
    /// </summary>
    private async Task SeedCompanyAsync(bool moduleEnabled = true)
    {
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantMock.Object.TenantId.ToString(),
            Name = "TestCo",
            LegalName = "TestCo S.A.",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
        };
        _db.Companies.Add(company);
        _db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = company.Id,
            TenantId = company.TenantId,
            PalmTrackEnabled = moduleEnabled,
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Siembra un mapping activo tenant→org (empresa con org propia de PalmTrack).
    /// </summary>
    private void SeedOrgMapping(string palmOrgId = "org-test-1", Guid? tenantId = null)
    {
        _db.Set<ExternalIdentityMapping>().Add(new ExternalIdentityMapping
        {
            PalmTrackOrgId = palmOrgId,
            ZorvianTenantId = (tenantId ?? _tenantMock.Object.TenantId.Value).ToString(),
            PalmTrackOrgName = "PalmTest Org",
            ZorvianTenantName = "TestCo",
            IsActive = true,
            LastSyncedAt = DateTime.UtcNow,
        });
        _db.SaveChanges();
    }

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responder(request));
        }
    }

    private PalmTrackReadController CreateController(
        IConfiguration config,
        FakeHttpMessageHandler handler,
        Mock<ITenantContext>? tenantMock = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var controller = new PalmTrackReadController(
            factory.Object, config, _logger.Object, tenantMock?.Object ?? _tenantMock.Object, _db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> ConfigWith(
        string? defaultOrgId = "global",
        string? platformEnabled = "false") =>
        new()
        {
            ["PalmTrack:ReadApiBaseUrl"] = "https://api.palmtrack-corp.com/api/palm/v1",
            ["PalmTrack:ReadApiKey"] = "key-123",
            ["PalmTrack:AllowedReadHosts:0"] = "api.palmtrack-corp.com",
            ["PalmTrack:DefaultOrgId"] = defaultOrgId,
            ["PalmTrack:Enabled"] = platformEnabled,
        };

    private static void SetQuery(PalmTrackReadController controller, string key, string value)
    {
        controller.HttpContext.Request.Query = new QueryCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                [key] = value,
            });
    }

    // ── Whitelist de hosts y proxy ──

    [Fact]
    public async Task GetFarms_AllowedHostFromConfig_ProxiesRequestWithKeyAndParsesItems()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"data":[{"id":"f1","name":"Finca A","organizationId":"org-1"}]}"""),
            });
        await SeedCompanyAsync();
        SeedOrgMapping("org-test-1");
        var controller = CreateController(BuildConfig(ConfigWith()), handler);

        var result = await controller.GetFarms();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.Host.Should().Be("api.palmtrack-corp.com");
        handler.LastRequest.RequestUri.AbsolutePath.Should().Contain("/api/palm/v1/farms");
        handler.LastRequest.RequestUri.Query.Should().Contain("orgId=org-test-1");
        handler.LastRequest.Headers.GetValues("X-PalmTrack-API-Key")
            .Should().Contain("key-123");
    }

    [Fact]
    public async Task GetFarms_CommaSeparatedAllowedHosts_ProxiesRequest()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PalmTrack:ReadApiBaseUrl"] = "https://api.palmtrack-corp.com/api/palm/v1",
            ["PalmTrack:ReadApiKey"] = "key-123",
            ["PalmTrack:AllowedReadHosts"] = "api.palmtrack-corp.com,other.example.com",
            ["PalmTrack:DefaultOrgId"] = "global",
            ["PalmTrack:Enabled"] = "false",
        });
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        await SeedCompanyAsync();
        SeedOrgMapping();
        var controller = CreateController(config, handler);

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Host.Should().Be("api.palmtrack-corp.com");
    }

    [Fact]
    public async Task GetFarms_HostNotAllowed_Returns503()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PalmTrack:ReadApiBaseUrl"] = "https://evil.example.com/api",
            ["PalmTrack:ReadApiKey"] = "key-123",
            ["PalmTrack:AllowedReadHosts:0"] = "api.palmtrack-corp.com",
            ["PalmTrack:DefaultOrgId"] = "global",
            ["PalmTrack:Enabled"] = "false",
        });
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        await SeedCompanyAsync();
        var controller = CreateController(config, handler);

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(503);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("not a whitelisted PalmTrack URL");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetFarms_DefaultsToPalmtrackHosts_WhenNotConfigured()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["PalmTrack:ReadApiBaseUrl"] = "https://palmtracklatam.com/api/palm/v1",
            ["PalmTrack:ReadApiKey"] = "key-123",
            // Sin AllowedReadHosts → usa los defaults
            ["PalmTrack:DefaultOrgId"] = "global",
            ["PalmTrack:Enabled"] = "false",
        });
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        await SeedCompanyAsync();
        SeedOrgMapping();
        var controller = CreateController(config, handler);

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Host.Should().Be("palmtracklatam.com");
    }

    // ── Gate del módulo (activación por empresa) ──

    [Fact]
    public async Task GetFarms_ModuleDisabledForCompany_Returns403()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        await SeedCompanyAsync(moduleEnabled: false);
        var controller = CreateController(BuildConfig(ConfigWith()), handler);

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(403);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("palmtrack_module_disabled");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetFarms_NoCompanyRow_PlatformConfigEnabled_Proceeds()
    {
        // Sin fila Company (tenant sin empresa registrada): fallback a PalmTrack:Enabled
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}"""),
            });
        var controller = CreateController(BuildConfig(ConfigWith(platformEnabled: "true")), handler);

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=global");
    }

    // ── Resolución de orgId ──

    [Fact]
    public async Task GetFarms_NoMapping_UsesDefaultOrgIdFromConfig()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[]}"""),
            });
        await SeedCompanyAsync();
        // Sin mapping → el org de plataforma (DefaultOrgId) aplica
        var controller = CreateController(BuildConfig(ConfigWith(defaultOrgId: "global")), handler);

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=global");
    }

    [Fact]
    public async Task GetFarms_NoMappingAndNoDefaultOrgId_Returns400NotConfigured()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        await SeedCompanyAsync();
        var controller = CreateController(BuildConfig(ConfigWith(defaultOrgId: null)), handler);

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("palmtrack_org_not_configured");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetFarms_NormalUserWithExplicitOrgId_IgnoresItAndUsesMapping()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        await SeedCompanyAsync();
        SeedOrgMapping("org-mapped");
        var controller = CreateController(BuildConfig(ConfigWith()), handler);
        SetQuery(controller, "orgId", "otro-org");

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=org-mapped");
        handler.LastRequest.RequestUri.Query.Should().NotContain("orgId=otro-org");
    }

    // ── Super admins ──

    [Fact]
    public async Task GetFarms_SuperAdminWithExplicitOrgId_UsesRequestedOrg()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        // Sin empresa ni mapping: el super admin opera a nivel de plataforma
        var controller = CreateController(BuildConfig(ConfigWith()), handler, tenantMock);
        SetQuery(controller, "orgId", "global");

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=global");
    }

    [Fact]
    public async Task GetFarms_SuperAdminWithoutExplicitOrgId_AggregatesAllKnownOrgs()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        // Dos orgs conocidas: una mapeada a una empresa + la default de plataforma
        SeedOrgMapping("org-empresa-1");

        var orgsSeen = new List<string>();
        var handler = new FakeHttpMessageHandler(req =>
        {
            var q = req.RequestUri!.Query;
            var match = System.Text.RegularExpressions.Regex.Match(q, @"orgId=([^&]+)");
            var orgId = match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : "?";
            orgsSeen.Add(orgId);
            var payload = "{\"data\":[{\"id\":\"f-" + orgId + "\",\"name\":\"Finca " + orgId + "\"}]}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload),
            };
        });
        var controller = CreateController(BuildConfig(ConfigWith(defaultOrgId: "global")), handler, tenantMock);

        var result = await controller.GetFarms();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        // Agregó items de ambas orgs
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("aggregated").GetBoolean().Should().BeTrue();
        orgsSeen.Should().Contain("org-empresa-1");
        orgsSeen.Should().Contain("global");
    }

    [Fact]
    public async Task GetFarms_SuperAdminAggregated_PartialOrgFailure_DoesNotBreakRest()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        SeedOrgMapping("org-empresa-1");

        var handler = new FakeHttpMessageHandler(req =>
        {
            var match = System.Text.RegularExpressions.Regex.Match(req.RequestUri!.Query, @"orgId=([^&]+)");
            var orgId = match.Success ? match.Groups[1].Value : "?";
            // La org mapeada falla; la default responde bien
            if (orgId == "org-empresa-1")
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("""{"error":"internal_error"}"""),
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"f-g"}]}"""),
            };
        });
        var controller = CreateController(BuildConfig(ConfigWith(defaultOrgId: "global")), handler, tenantMock);

        var result = await controller.GetFarms();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
        var orgErrors = doc.RootElement.GetProperty("orgErrors");
        orgErrors.TryGetProperty("org-empresa-1", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetFarms_SuperAdminWithoutMappingOrOrgIdOrDefault_Returns400NotConfigured()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var controller = CreateController(BuildConfig(ConfigWith(defaultOrgId: null)), handler, tenantMock);

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("palmtrack_org_not_configured");
        // El mensaje orienta al admin a configurar el org de plataforma
        json.Should().Contain("PalmTrack:DefaultOrgId");
    }
}
