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
        // Flujos anónimos de credenciales: el cliente no tiene token, así que no
        // puede enviar Authorization ni un header anti-CSRF.
        "/zorvian/v1/auth/forgot-password",
        "/zorvian/v1/auth/reset-password",
        "/zorvian/v1/auth/register",
        "/zorvian/v1/auth/request-access",
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
                // Las API Keys viajan en `X-API-Key`, NO en `Authorization`: sin
                // esta excepcion, toda llamada por API Key con verbo no seguro
                // (POST/PUT/DELETE) devolvia 403. Es aceptable porque la key no
                // vive en cookies: un sitio tercero no puede leerla ni forzar al
                // navegador a enviarla (no es un header CORS-simple).
                var hasCustomHeader = context.Request.Headers["X-Requested-With"].Any()
                    || context.Request.Headers["X-CSRF-Protected"].Any()
                    || context.Request.Headers["X-CSRF-Token"].Any()
                    || context.Request.Headers["X-API-Key"].Any()
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
