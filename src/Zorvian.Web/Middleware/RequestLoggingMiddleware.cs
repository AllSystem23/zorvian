using System.Diagnostics;

namespace Zorvian.Web.Middleware;

/// <summary>
/// Logs incoming requests with timing, correlation ID, and user info.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    private static readonly HashSet<string> ExcludePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health", "/health/ready", "/health/live", "/favicon.ico"
    };

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip health checks and static files
        if (ExcludePaths.Contains(path) || path.StartsWith("/swagger") || path.StartsWith("/openapi"))
        {
            await _next(context);
            return;
        }

        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.TraceIdentifier;
        var method = context.Request.Method;
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;
            var statusCode = context.Response.StatusCode;

            // Log level based on status code
            if (statusCode >= 500)
            {
                _logger.LogError(
                    "[{Method}] {Path} → {Status} ({Elapsed}ms) | IP={IP} User={User} Correlation={CorrId}",
                    method, path, statusCode, elapsed, remoteIp, userId ?? "anonymous", correlationId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "[{Method}] {Path} → {Status} ({Elapsed}ms) | IP={IP} User={User} Correlation={CorrId}",
                    method, path, statusCode, elapsed, remoteIp, userId ?? "anonymous", correlationId);
            }
            else if (elapsed > 1000)
            {
                // Log slow requests (>1s) even if successful
                _logger.LogWarning(
                    "[SLOW] [{Method}] {Path} → {Status} ({Elapsed}ms) | IP={IP} User={User} Correlation={CorrId}",
                    method, path, statusCode, elapsed, remoteIp, userId ?? "anonymous", correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "[{Method}] {Path} → {Status} ({Elapsed}ms) | Correlation={CorrId}",
                    method, path, statusCode, elapsed, correlationId);
            }
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}