using System.IdentityModel.Tokens.Jwt;
using Zorvian.Application.Interfaces;
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
        // 1. Extraer tenant_id del JWT o header
        if (tenantContext.TenantId.Value == Guid.Empty)
        {
            var tenantId = context.User?.FindFirst("tenant_id")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(tenantId))
            {
                tenantWriter.SetTenantId(tenantId);
            }
        }

        // 2. Resolver usuario y empleado
        var userIdClaim = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var employeeIdClaim = context.User?.FindFirst("employee_id")?.Value;

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
        Guid? employeeId = Guid.TryParse(employeeIdClaim, out var eid) ? eid : null;
        tenantWriter.SetCurrentUser(userId, employeeId);

        // 3. Detectar SuperAdmin. Se valida cualquier rol SuperAdmin en el JWT,
        // no solo el rol primario, para usuarios con múltiples roles.
        var isSuperAdmin = context.User?.Claims.Any(c =>
            (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "SuperAdmin") == true;
        tenantWriter.SetIsSuperAdmin(isSuperAdmin);

        // 4. Auto-cargar primera empresa para SuperAdmin si no tiene empresa seleccionada
        if (isSuperAdmin && tenantContext.TenantId.Value == Guid.Empty)
        {
            using var scope = context.RequestServices.CreateScope();
            var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var companies = await authRepo.GetAllCompaniesAsync();
            var firstCompany = companies.FirstOrDefault(c => !c.IsDeleted);

            if (firstCompany is not null)
            {
                tenantWriter.SetTenantId(firstCompany.TenantId);
                logger.LogInformation("SuperAdmin auto-loaded company: {CompanyName} (TenantId: {TenantId})", firstCompany.Name, firstCompany.TenantId);
            }
            else
            {
                logger.LogWarning("SuperAdmin has no companies in the system");
            }
        }

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
