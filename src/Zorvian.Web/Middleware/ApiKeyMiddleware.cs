using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Zorvian.Infrastructure.Services;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Middleware;

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
        var tenantId = await apiKeyService.ValidateKeyAsync(extractedApiKey!);

        if (tenantId != null)
        {
            var tenantWriter = context.RequestServices.GetRequiredService<ITenantContextWriter>();
            tenantWriter.SetTenantId(tenantId);

            context.Items["IsApiKeyAuth"] = true;
        }
        else
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API Key inválida o expirada" });
            return;
        }

        await _next(context);
    }
}
