using System.IdentityModel.Tokens.Jwt;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantContextWriter tenantWriter,
        ILogger<TenantMiddleware> logger)
    {
        // 1. Resolver tenant (compañía) desde el claim del JWT o el header X-Tenant-Id.
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

        // 2. Resolver usuario y empleado.
        //    No pisar el usuario ya fijado por ApiKeyMiddleware cuando el
        //    request no trae claims JWT (flujo API Key): solo sobrescribir si
        //    hay claims o no hay usuario previo.
        var userIdClaim = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var employeeIdClaim = context.User?.FindFirst("employee_id")?.Value;

        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;
        Guid? employeeId = Guid.TryParse(employeeIdClaim, out var eid) ? eid : null;

        if (userId is not null || employeeId is not null || tenantContext.CurrentUserId is null)
        {
            tenantWriter.SetCurrentUser(userId, employeeId);
        }

        // 3. Detectar SuperAdmin.
        var isSuperAdmin = context.User?.Claims.Any(c =>
            (c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role") && c.Value == "SuperAdmin") == true;
        tenantWriter.SetIsSuperAdmin(isSuperAdmin);

        // 4. Auto-cargar primera empresa para SuperAdmin si no tiene empresa seleccionada.
        //    Esto fija una compañía concreta → BypassTenantFilter = false → lecturas acotadas.
        if (isSuperAdmin && !tenantContext.HasValidCompany())
        {
            using var scope = context.RequestServices.CreateScope();
            var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
            var companies = await authRepo.GetAllCompaniesAsync();
            var firstCompany = companies.FirstOrDefault(c => !c.IsDeleted);

            if (firstCompany is not null)
            {
                var companyTenantId = Guid.TryParse(firstCompany.TenantId, out var guid)
                    ? new TenantId(guid)
                    : TenantId.FromGuid(firstCompany.Id);

                tenantWriter.SetTenantId(companyTenantId);
            }
        }

        // 5. Resolver y validar la sucursal seleccionada (header X-Branch-Id).
        //    Se valida que la sucursal pertenezca a la compañía operativa para evitar
        //    inyección cross-company de BranchId. Si es inválida o no aplica → null.
        await ResolveBranchAsync(context, tenantContext, tenantWriter, logger);

        await _next(context);
    }

    private static async Task ResolveBranchAsync(
        HttpContext context,
        ITenantContext tenantContext,
        ITenantContextWriter tenantWriter,
        ILogger<TenantMiddleware> logger)
    {
        // Sin header de sucursal → sin alcance de sucursal (todas).
        var branchHeader = context.Request.Headers["X-Branch-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(branchHeader))
        {
            tenantWriter.SetBranchId(null);
            return;
        }

        if (!Guid.TryParse(branchHeader, out var branchId) || branchId == Guid.Empty)
        {
            tenantWriter.SetBranchId(null);
            return;
        }

        // Sin compañía concreta seleccionada no hay referencia válida para validar.
        if (tenantContext.SelectedCompanyId is not { } companyId)
        {
            logger.LogWarning(
                "X-Branch-Id={BranchId} ignorado: no hay compañía seleccionada (tenant={Tenant}).",
                branchId, tenantContext.TenantId);
            tenantWriter.SetBranchId(null);
            return;
        }

        using var scope = context.RequestServices.CreateScope();
        var branchRepo = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var branches = await branchRepo.GetAllAsync(companyId);

        if (branches.Any(b => b.Id == branchId))
        {
            tenantWriter.SetBranchId(branchId);
        }
        else
        {
            logger.LogWarning(
                "X-Branch-Id={BranchId} rechazado: no pertenece a la compañía {CompanyId}.",
                branchId, companyId);
            tenantWriter.SetBranchId(null);
        }
    }
}

public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantMiddleware>();
    }
}
