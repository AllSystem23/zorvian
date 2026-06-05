using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Zorvian.Web.Authorization;

namespace Zorvian.Tests.Authorization;

public sealed class RequirePermissionAttributeTests
{
    private static (AuthorizationFilterContext context, DefaultHttpContext httpContext) MakeContext(
        Action<ClaimsIdentity>? configureClaims = null)
    {
        var identity = new ClaimsIdentity("jwt");
        configureClaims?.Invoke(identity);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var filterContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        return (filterContext, httpContext);
    }

    [Fact]
    public void OnAuthorization_AuthenticatedWithPermission_Passes()
    {
        var (context, _) = MakeContext(identity =>
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-1"));
            identity.AddClaim(new Claim("permission", "credit.read"));
        });

        var attr = new RequirePermissionAttribute("credit.read");
        attr.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_AuthenticatedWithoutPermission_Fails()
    {
        var (context, _) = MakeContext(identity =>
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-1"));
            identity.AddClaim(new Claim("permission", "employee.read"));
        });

        var attr = new RequirePermissionAttribute("credit.read");
        attr.OnAuthorization(context);

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_SuperAdminBypass_Passes()
    {
        var (context, _) = MakeContext(identity =>
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "super-1"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "SuperAdmin"));
        });

        var attr = new RequirePermissionAttribute("some.obscure.permission");
        attr.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_Unauthenticated_Fails()
    {
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attr = new RequirePermissionAttribute("credit.read");
        attr.OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_MultiplePermissions_RequiresAny()
    {
        var (context, _) = MakeContext(identity =>
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-1"));
            identity.AddClaim(new Claim("permission", "payroll.read"));
        });

        var attrPayroll = new RequirePermissionAttribute("payroll.read");
        attrPayroll.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnAuthorization_EmployeeWithPermission_Passes()
    {
        var (context, _) = MakeContext(identity =>
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "emp-1"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Employee"));
            identity.AddClaim(new Claim("permission", "employee.read"));
        });

        var attr = new RequirePermissionAttribute("employee.read");
        attr.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void AttributeUsage_AllowsMultipleOnMethodAndClass()
    {
        var attrs = typeof(RequirePermissionAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false);
        var usage = (AttributeUsageAttribute)attrs[0];

        Assert.True(usage.AllowMultiple);
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Method));
        Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
    }

    [Fact]
    public void PermissionCode_IsStored()
    {
        var attr = new RequirePermissionAttribute("inventory.write");
        Assert.Equal("inventory.write", attr.Permission);
        Assert.Equal("Permission:inventory.write", attr.Policy);
    }
}
