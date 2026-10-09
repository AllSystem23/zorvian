using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AuditAttribute : ActionFilterAttribute
{
    private readonly string _entityName;
    private readonly string _action;

    public AuditAttribute(string entityName, string action)
    {
        _entityName = entityName;
        _action = action;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var tenant = httpContext.RequestServices.GetRequiredService<ITenantContext>();
        var db = httpContext.RequestServices.GetRequiredService<ZorvianDbContext>();

        var userIdClaim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

        var entityId = context.RouteData.Values["id"]?.ToString() ?? "";

        // Log before execution — captures intent even if action fails
        var log = new AuditLog
        {
            TenantId = tenant.TenantId ?? "unknown",
            EntityName = _entityName,
            EntityId = entityId,
            Action = _action,
            PerformedBy = userId,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            RequestPath = httpContext.Request.Path,
        };

        db.AuditLogs.Add(log);

        var result = await next();

        // AUD-004: los AuditLog son inmutables una vez persistidos. Si la acción
        // ya hizo SaveChanges (log insertado, estado Unchanged), mutar el log
        // haría que el interceptor de inmutabilidad lanzara AUD-004 y ENMASCARARA
        // el error original. En ese caso registramos la falla en un log nuevo.
        if (db.Entry(log).State == EntityState.Added)
        {
            // Aún no persistido: completar campos es seguro (sigue siendo Added).
            var newEntityId = context.RouteData.Values["id"]?.ToString() ?? "";
            if (!string.IsNullOrEmpty(newEntityId) && string.IsNullOrEmpty(entityId))
            {
                log.EntityId = newEntityId;
            }

            if (result.Exception is not null && !result.ExceptionHandled)
            {
                log.OldValues = $"Failed: {result.Exception.Message}";
            }
        }
        else if (result.Exception is not null && !result.ExceptionHandled)
        {
            // Ya persistido: registrar el fallo como un registro nuevo (inserción permitida).
            db.AuditLogs.Add(new AuditLog
            {
                TenantId = log.TenantId,
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                Action = log.Action,
                OldValues = $"Failed: {result.Exception.Message}",
                PerformedBy = log.PerformedBy,
                IpAddress = log.IpAddress,
                UserAgent = log.UserAgent,
                RequestPath = log.RequestPath,
            });
        }

        await db.SaveChangesAsync();
    }
}
