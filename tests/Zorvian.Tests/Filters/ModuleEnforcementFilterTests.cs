using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Zorvian.Application.Services;
using Zorvian.Core.Enums;
using Zorvian.Web.Filters;

namespace Zorvian.Tests.Filters;

public sealed class ModuleEnforcementFilterTests
{
    private static (AuthorizationFilterContext context, Mock<IModuleEntitlementService> service) Build(
        string controller, string? role = "CompanyAdmin", string? planId = "starter", bool authenticated = true)
    {
        var entitlement = new Mock<IModuleEntitlementService>();
        var services = new ServiceCollection();
        services.AddSingleton(entitlement.Object);
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };

        if (authenticated)
        {
            var claims = new List<Claim>();
            if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
            if (planId != null) claims.Add(new Claim("plan_id", planId));
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        var routeData = new RouteData();
        if (controller.Length > 0) routeData.Values["controller"] = controller;

        var actionContext = new ActionContext(httpContext, routeData, new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        return (context, entitlement);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Allows_WhenModuleEnabled()
    {
        var (context, entitlement) = Build("Sales", planId: "starter");
        entitlement.Setup(s => s.IsModuleEnabledAsync("starter", ModuleType.Sales)).ReturnsAsync(true);

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Forbids_WhenModuleDisabled()
    {
        var (context, entitlement) = Build("Vehicles", planId: "starter");
        entitlement.Setup(s => s.IsModuleEnabledAsync("starter", ModuleType.Fleet)).ReturnsAsync(false);

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Skips_SuperAdmin()
    {
        var (context, entitlement) = Build("Vehicles", role: "SuperAdmin", planId: "starter");

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        entitlement.Verify(s => s.IsModuleEnabledAsync(It.IsAny<string>(), It.IsAny<ModuleType>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Skips_UnmappedController()
    {
        var (context, entitlement) = Build("Dashboard", planId: "starter");

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
        entitlement.Verify(s => s.IsModuleEnabledAsync(It.IsAny<string>(), It.IsAny<ModuleType>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_Skips_Unauthenticated()
    {
        var (context, _) = Build("Vehicles", authenticated: false);

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OnAuthorizationAsync_DefaultsToStarter_WhenPlanClaimMissing()
    {
        var (context, entitlement) = Build("Vehicles", planId: null);
        entitlement.Setup(s => s.IsModuleEnabledAsync("starter", ModuleType.Fleet)).ReturnsAsync(false);

        await new ModuleEnforcementFilter().OnAuthorizationAsync(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(403, result.StatusCode);
        entitlement.Verify(s => s.IsModuleEnabledAsync("starter", ModuleType.Fleet), Times.Once);
    }
}
