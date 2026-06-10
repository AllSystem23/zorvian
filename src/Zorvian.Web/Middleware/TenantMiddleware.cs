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

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantContextWriter tenantWriter, ILogger<TenantMiddleware> logger)
    {
        if (tenantContext.TenantId.Value == Guid.Empty)
        {
            var tenantId = context.User?.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            }

            logger.LogInformation("TenantId from claim/header: {TenantId}", tenantId);
            if (!string.IsNullOrEmpty(tenantId))
            {
                tenantWriter.SetTenantId(tenantId);
            }
        }

        var userIdClaim = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var employeeIdClaim = context.User?.FindFirst("employee_id")?.Value;

        logger.LogInformation("UserId: {UserId}, EmployeeId: {EmployeeId}", userIdClaim, employeeIdClaim);

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
        Guid? employeeId = Guid.TryParse(employeeIdClaim, out var eid) ? eid : null;
        tenantWriter.SetCurrentUser(userId, employeeId);

        var roleClaim = context.User?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        tenantWriter.SetIsSuperAdmin(roleClaim == "SuperAdmin");

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
