using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
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

        // After execution, update EntityId for create actions where id is set in the response
        var newEntityId = context.RouteData.Values["id"]?.ToString() ?? "";
        if (!string.IsNullOrEmpty(newEntityId) && string.IsNullOrEmpty(entityId))
        {
            log.EntityId = newEntityId;
        }

        if (result.Exception is not null && !result.ExceptionHandled)
        {
            log.OldValues = $"Failed: {result.Exception.Message}";
        }

        await db.SaveChangesAsync();
    }
}
