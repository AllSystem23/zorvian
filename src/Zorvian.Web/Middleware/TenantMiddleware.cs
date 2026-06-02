using System.IdentityModel.Tokens.Jwt;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        // Only try to set from JWT if not already set by another middleware (like ApiKey)
        if (string.IsNullOrEmpty(tenantContext.TenantId))
        {
            var tenantId = context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                tenantContext.SetTenantId(tenantId);
            }
        }

        var userIdClaim = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var employeeIdClaim = context.User?.FindFirst("employee_id")?.Value;

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
        Guid? employeeId = Guid.TryParse(employeeIdClaim, out var eid) ? eid : null;
        tenantContext.SetCurrentUser(userId, employeeId);

        await _next(context);
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
