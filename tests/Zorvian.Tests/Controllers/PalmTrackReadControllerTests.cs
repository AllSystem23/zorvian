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
/// Tests para PalmTrackReadController: verifica la whitelist configurable
/// de hosts (PalmTrack:AllowedReadHosts), el reenvío de la API key y el
/// 503 cuando el host no está permitido.
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
    /// Siembra un mapping activo tenant→org: los endpoints exigen orgId,
    /// resuelto vía ExternalIdentityMapping antes de llamar a PalmTrack.
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

    private PalmTrackReadController CreateControllerForTenant(
        Mock<ITenantContext> tenantMock,
        IConfiguration config,
        FakeHttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var controller = new PalmTrackReadController(factory.Object, config, _logger.Object, tenantMock.Object, _db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
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
        FakeHttpMessageHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(handler));

        var controller = new PalmTrackReadController(factory.Object, config, _logger.Object, _tenantMock.Object, _db);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static readonly Dictionary<string, string?> BaseConfig = new()
    {
        ["PalmTrack:ReadApiBaseUrl"] = "https://api.palmtrack-corp.com/api/palm/v1",
        ["PalmTrack:ReadApiKey"] = "key-123",
        ["PalmTrack:AllowedReadHosts:0"] = "api.palmtrack-corp.com",
    };

    [Fact]
    public async Task GetFarms_AllowedHostFromConfig_ProxiesRequestWithKeyAndParsesItems()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"success":true,"data":[{"id":"f1","name":"Finca A","organizationId":"org-1"}]}"""),
            });
        SeedOrgMapping("org-test-1");
        var controller = CreateController(BuildConfig(BaseConfig), handler);

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
        });
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
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
        });
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
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
            ["PalmTrack:ReadApiBaseUrl"] = "https://palmtracklatam.com/api",
            ["PalmTrack:ReadApiKey"] = "key-123",
            // Sin AllowedReadHosts → usa los defaults
        });
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        SeedOrgMapping();
        var controller = CreateController(config, handler);

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Host.Should().Be("palmtracklatam.com");
    }

    [Fact]
    public async Task GetFarms_NoActiveMapping_Returns400WithPalmtrackOrgNotMapped()
    {
        var config = BuildConfig(BaseConfig);
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
        // Sin SeedOrgMapping: el tenant no tiene mapping reconciliado
        var controller = CreateController(config, handler);

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("palmtrack_org_not_mapped");
        handler.LastRequest.Should().BeNull();
    }

    [Fact]
    public async Task GetFarms_SuperAdminWithExplicitOrgId_UsesRequestedOrg()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        var config = BuildConfig(BaseConfig);
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        // Sin mapping: el super admin depende del orgId explícito
        var controller = CreateControllerForTenant(tenantMock, config, handler);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
        };
        controller.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["orgId"] = "global",
        });

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=global");
    }

    [Fact]
    public async Task GetFarms_NormalUserWithExplicitOrgId_IgnoresItAndUsesMapping()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":[{"id":"x"}]}"""),
            });
        SeedOrgMapping("org-mapped");
        var controller = CreateController(BuildConfig(BaseConfig), handler);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
        };
        // Intento de cross-tenant: se debe ignorar el orgId del query
        controller.HttpContext.Request.Query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["orgId"] = "otro-org",
        });

        var result = await controller.GetFarms();

        result.Should().BeOfType<OkObjectResult>();
        handler.LastRequest!.RequestUri!.Query.Should().Contain("orgId=org-mapped");
        handler.LastRequest.RequestUri.Query.Should().NotContain("orgId=otro-org");
    }

    [Fact]
    public async Task GetFarms_SuperAdminWithoutMappingOrOrgId_Returns400WithHint()
    {
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        tenantMock.Setup(t => t.IsSuperAdmin).Returns(true);

        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK));
        var controller = CreateControllerForTenant(tenantMock, BuildConfig(BaseConfig), handler);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext(),
        };

        var result = await controller.GetFarms();

        var obj = result.Should().BeOfType<ObjectResult>().Subject;
        obj.StatusCode.Should().Be(400);
        var json = JsonSerializer.Serialize(obj.Value);
        json.Should().Contain("palmtrack_org_not_mapped");
        json.Should().Contain("?orgId=");
    }
}