using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Middleware;

/// <summary>
/// Autenticación por API Key (header X-API-Key). Corre ANTES de
/// UseAuthentication/UseTenantMiddleware: fija el tenant de la key y fabrica
/// un ClaimsPrincipal autenticado que hereda roles y permisos del usuario
/// dueño de la key (igual que el JWT), para que [Authorize] y
/// RequirePermission funcionen en el flujo API Key.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-Key";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            await _next(context);
            return;
        }

        var apiKeyService = context.RequestServices.GetRequiredService<ApiKeyService>();
        var apiKeyInfo = await apiKeyService.ValidateKeyAndGetInfoAsync(extractedApiKey!);

        if (apiKeyInfo is null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key inválida o expirada" });
            return;
        }

        var (tenantId, userId) = apiKeyInfo.Value;

        var tenantWriter = context.RequestServices.GetRequiredService<ITenantContextWriter>();
        tenantWriter.SetTenantId(TenantId.FromString(tenantId));
        tenantWriter.SetCurrentUser(userId, null);

        context.User = await BuildPrincipalAsync(context, tenantId, userId);
        context.Items["IsApiKeyAuth"] = true;

        await _next(context);
    }

    private static async Task<ClaimsPrincipal> BuildPrincipalAsync(
        HttpContext context, string tenantId, Guid? userId)
    {
        var identity = new ClaimsIdentity(authenticationType: "ApiKey");
        identity.AddClaim(new Claim("tenant_id", tenantId));

        // Key sin usuario dueño: solo tenant. Pasará [Authorize] pero
        // RequirePermission denegará por defecto (sin claims permission).
        if (userId is not { } uid)
            return new ClaimsPrincipal(identity);

        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, uid.ToString()));

        using var scope = context.RequestServices.CreateScope();
        var authRepo = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
        var user = await authRepo.GetUserWithRolesAsync(uid);

        if (user is null)
            return new ClaimsPrincipal(identity);

        foreach (var roleName in user.UserRoles
            .Select(ur => ur.Role.Name.ToString())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            identity.AddClaim(new Claim("role", roleName));
        }

        foreach (var permission in user.UserRoles
            .Select(ur => ur.Role)
            .SelectMany(role => role.RolePermissions ?? [])
            .Select(p => p.PermissionCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct())
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        return new ClaimsPrincipal(identity);
    }
}
