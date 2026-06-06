namespace Zorvian.Web.Middleware;

public sealed class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    private static readonly HashSet<string> SafeMethods = ["GET", "HEAD", "OPTIONS", "TRACE"];

    private static readonly string[] PublicPrefixes =
    [
        "/zorvian/v1/auth/login",
        "/zorvian/v1/auth/mfa/login",
        "/zorvian/v1/auth/refresh",
        "/zorvian/v1/antiforgery",
        "/zorvian/v1/health",
        "/health",
        "/swagger",
        "/openapi",
    ];

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SafeMethods.Contains(context.Request.Method))
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            if (!PublicPrefixes.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
            {
                var hasCustomHeader = context.Request.Headers["X-Requested-With"].Any()
                    || context.Request.Headers["X-CSRF-Protected"].Any()
                    || context.Request.Headers["X-CSRF-Token"].Any()
                    || context.Request.Headers.Authorization.Any();

                if (!hasCustomHeader)
                {
                    context.Response.StatusCode = 403;
                    context.Response.Headers["X-CSRF-Failure"] = "true";
                    await context.Response.WriteAsync("{\"error\":\"CSRF validation: missing required header\"}");
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class CsrfMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfMiddleware>();
    }
}
