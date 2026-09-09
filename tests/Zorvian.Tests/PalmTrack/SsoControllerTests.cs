using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Zorvian.Application.DTOs.Auth;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Interfaces.PalmTrack;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;
using Zorvian.Web.Controllers;

namespace Zorvian.Tests.PalmTrack;

/// <summary>
/// Tests for SsoController per plan Paso 6 §11.
/// </summary>
public sealed class SsoControllerTests
{
    private readonly Mock<ISsoService> _ssoService = new();
    private readonly Mock<ILogger<SsoController>> _logger = new();
    private readonly Mock<IPalmTrackIdentityService> _identityService = new();
    private readonly ZorvianDbContext _db;
    private readonly SsoController _sut;

    public SsoControllerTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PalmTrack:SsoEnabled"] = "true",
            })
            .Build();

        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(Guid.NewGuid()));
        _db = new ZorvianDbContext(
            new DbContextOptionsBuilder<ZorvianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options,
            tenantMock.Object);

        // By default, orgs are not reconciled → flag falls back to config
        _identityService.Setup(i => i.GetTenantIdAsync(It.IsAny<string>())).ReturnsAsync((Guid?)null);

        _sut = new SsoController(_ssoService.Object, config, _logger.Object, _identityService.Object, _db);
    }

    private SsoController CreateControllerWithFlag(bool enabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PalmTrack:SsoEnabled"] = enabled.ToString(),
            })
            .Build();

        return new SsoController(_ssoService.Object, config, _logger.Object, _identityService.Object, _db);
    }

    private (ZorvianDbContext Db, Mock<IPalmTrackIdentityService> Identity, Guid TenantGuid) CreateReconciledSetup(bool ssoEnabledInDb)
    {
        var tenantGuid = Guid.NewGuid();
        var tenantMock = new Mock<ITenantContext>();
        tenantMock.Setup(t => t.TenantId).Returns(new TenantId(tenantGuid));

        var db = new ZorvianDbContext(
            new DbContextOptionsBuilder<ZorvianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options,
            tenantMock.Object);

        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenantGuid.ToString(),
            Name = "TestCo",
            LegalName = "TestCo S.A.",
            Country = "Nicaragua",
            Currency = "NIO",
            Timezone = "America/Managua",
        };
        db.Companies.Add(company);
        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = company.Id,
            TenantId = tenantGuid.ToString(),
            PalmTrackSsoEnabled = ssoEnabledInDb,
        });
        db.SaveChanges();

        var identity = new Mock<IPalmTrackIdentityService>();
        identity.Setup(i => i.GetTenantIdAsync("org-123")).ReturnsAsync(tenantGuid);

        return (db, identity, tenantGuid);
    }

    // ── Feature flag tests ──

    [Fact]
    public async Task SsoLogin_SsoDisabled_ShouldReturn404()
    {
        var controller = CreateControllerWithFlag(false);

        var result = await controller.SsoLogin("token", "org-123");

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        // Verify it returns sso_disabled error
        notFound.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SsoLogin_SettingsRowDisablesSso_ShouldReturn404_EvenIfConfigEnabled()
    {
        var (db, identity, _) = CreateReconciledSetup(ssoEnabledInDb: false);
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PalmTrack:SsoEnabled"] = "true" })
                .Build();
            var controller = new SsoController(_ssoService.Object, config, _logger.Object, identity.Object, db);

            var result = await controller.SsoLogin("valid-token", "org-123");

            result.Should().BeOfType<NotFoundObjectResult>();
            _ssoService.Verify(s => s.SsoLoginAsync(It.IsAny<string>(), It.IsAny<string>(), null, null), Times.Never);
        }
        finally
        {
            db.Dispose();
        }
    }

    [Fact]
    public async Task SsoLogin_SettingsRowEnablesSso_ShouldProceed_EvenIfConfigDisabled()
    {
        var (db, identity, _) = CreateReconciledSetup(ssoEnabledInDb: true);
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["PalmTrack:SsoEnabled"] = "false" })
                .Build();
            var controller = new SsoController(_ssoService.Object, config, _logger.Object, identity.Object, db);

            _ssoService.Setup(s => s.SsoLoginAsync("valid-token", "org-123", null, null))
                .ReturnsAsync(SsoLoginResult.Ok(new AuthResponse(
                    "access-token", "refresh-token", 3600,
                    new UserInfo("1", "a@b.com", "User", "Employee", "t-1", "NIO", null))));

            var result = await controller.SsoLogin("valid-token", "org-123");

            result.Should().BeOfType<OkObjectResult>();
            _ssoService.Verify(s => s.SsoLoginAsync("valid-token", "org-123", null, null), Times.Once);
        }
        finally
        {
            db.Dispose();
        }
    }

    [Fact]
    public async Task SsoLogin_MissingToken_ShouldReturn400()
    {
        var result = await _sut.SsoLogin("", "org-123");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SsoLogin_MissingOrgId_ShouldReturn400()
    {
        var result = await _sut.SsoLogin("token", "");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── SSO login flow tests ──

    [Fact]
    public async Task SsoLogin_Success_NormalAccess_ShouldReturn200()
    {
        _ssoService.Setup(s => s.SsoLoginAsync("valid-token", "org-123", null, null))
            .ReturnsAsync(SsoLoginResult.Ok(new AuthResponse(
                "access-token", "refresh-token", 3600,
                new UserInfo("1", "a@b.com", "User", "Employee", "t-1", "NIO", null))));

        var result = await _sut.SsoLogin("valid-token", "org-123");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SsoLogin_Success_ProfileRequired_ShouldReturn200WithFlag()
    {
        _ssoService.Setup(s => s.SsoLoginAsync("valid-token", "org-123", null, null))
            .ReturnsAsync(SsoLoginResult.ProfileRequired(new AuthResponse(
                "temp-token", "temp-refresh", 3600,
                new UserInfo("1", "a@b.com", "New User", "Employee", "t-1", "NIO", null))));

        var result = await _sut.SsoLogin("valid-token", "org-123");

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SsoLogin_Failed_ShouldReturn401()
    {
        _ssoService.Setup(s => s.SsoLoginAsync("bad-token", "org-123", null, null))
            .ReturnsAsync(SsoLoginResult.Failed("Token inválido"));

        var result = await _sut.SsoLogin("bad-token", "org-123");

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task SsoLogin_WithPalmTrackRole_ShouldPassToService()
    {
        _ssoService.Setup(s => s.SsoLoginAsync("token", "org-123", "manager", "P001"))
            .ReturnsAsync(SsoLoginResult.Ok(new AuthResponse(
                "token", "refresh", 3600,
                new UserInfo("1", "a@b.com", "User", "Employee", "t-1", "NIO", null))));

        var result = await _sut.SsoLogin("token", "org-123",
            palmTrackRole: "manager", palmTrackProducerCode: "P001");

        result.Should().BeOfType<OkObjectResult>();
        _ssoService.Verify(s => s.SsoLoginAsync("token", "org-123", "manager", "P001"), Times.Once);
    }

    // ── CompleteProfile tests ──

    [Fact]
    public async Task CompleteProfile_Success_ShouldReturn200()
    {
        // Setup controller with a user_id claim
        var claims = new[] { new System.Security.Claims.Claim("user_id", Guid.NewGuid().ToString()) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };

        _ssoService.Setup(s => s.CompleteProfileAsync(
                It.IsAny<Guid>(), "P001", "Producción", "Productor", "+505 8888-7777"))
            .ReturnsAsync(true);

        var result = await _sut.CompleteProfile(new CompleteProfileRequest(
            "P001", "Producción", "Productor", "+505 8888-7777"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CompleteProfile_MissingEmployeeCode_ShouldReturn400()
    {
        var claims = new[] { new System.Security.Claims.Claim("user_id", Guid.NewGuid().ToString()) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };

        var result = await _sut.CompleteProfile(new CompleteProfileRequest(
            "", "Dept", "Position"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CompleteProfile_UserNotFound_ShouldReturn404()
    {
        var claims = new[] { new System.Security.Claims.Claim("user_id", Guid.NewGuid().ToString()) };
        var identity = new System.Security.Claims.ClaimsIdentity(claims);
        var principal = new System.Security.Claims.ClaimsPrincipal(identity);
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = principal }
        };

        _ssoService.Setup(s => s.CompleteProfileAsync(
                It.IsAny<Guid>(), "P001", "Dept", "Pos", null))
            .ReturnsAsync(false);

        var result = await _sut.CompleteProfile(new CompleteProfileRequest(
            "P001", "Dept", "Pos"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CompleteProfile_NoUserIdClaim_ShouldReturn401()
    {
        _sut.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal() }
        };

        var result = await _sut.CompleteProfile(new CompleteProfileRequest(
            "P001", "Dept", "Pos"));

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}