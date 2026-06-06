using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zorvian.Web.Authorization;

/// <summary>
/// Atributo de autorización basado en permisos específicos del sistema.
/// No requiere registrar policies porque la validación se hace en OnAuthorization.
/// Se ejecuta con orden prioritario (Order = -1000) para validar la autenticación
/// ANTES que el AuthorizeAttribute del framework (que buscaría la policy registrada).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    public string Permission { get; }

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
    }

    public int Order => -1000;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Si el usuario no está autenticado, devolver 401 Unauthorized
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // SuperAdmin tiene todos los permisos
        if (user.IsInRole("SuperAdmin"))
            return;

        // Verificar que el usuario tiene el permiso requerido
        var hasPermission = user.HasClaim("permission", Permission);
        if (!hasPermission)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
