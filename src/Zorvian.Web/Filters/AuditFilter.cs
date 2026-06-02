using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

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
        var result = await next();

        if (result.Exception is not null)
            return;

        var httpContext = context.HttpContext;
        var tenant = httpContext.RequestServices.GetRequiredService<ITenantContext>();
        var repo = httpContext.RequestServices.GetRequiredService<IAuditLogRepository>();

        var userIdClaim = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = Guid.TryParse(userIdClaim, out var uid) ? uid : null;

        var log = new AuditLog
        {
            TenantId = tenant.TenantId ?? "unknown",
            EntityName = _entityName,
            EntityId = context.RouteData.Values["id"]?.ToString() ?? "",
            Action = _action,
            PerformedBy = userId,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers.UserAgent.ToString(),
            RequestPath = httpContext.Request.Path,
        };

        await repo.AddAsync(log);
    }
}
