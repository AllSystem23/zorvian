using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Zorvian.Web.Middleware;

public static class RateLimitingExtensions
{
    public static IApplicationBuilder UseRateLimitingMiddleware(this IApplicationBuilder builder, int maxRequests = 120, int windowSeconds = 60)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(maxRequests, windowSeconds);
    }
}

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, FixedWindowCounter> _clients = new();
    private readonly Timer _cleanupTimer;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 120, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;

        // Cleanup stale entries every 5 minutes
        _cleanupTimer = new Timer(CleanupStaleEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip rate limiting for health checks
        if (path.StartsWith("/health"))
        {
            await _next(context);
            return;
        }

        var clientKey = GetClientKey(context);

        // Stricter rate limiting for auth endpoints
        if (path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryRequest($"auth:{clientKey}", 5, 900))
            {
                await WriteRateLimitResponse(context, 429,
                    "Demasiados intentos de autenticación. Intenta de nuevo en 15 minutos.",
                    retryAfterSeconds: 900);
                return;
            }

            await _next(context);
            return;
        }

        // General rate limiting
        if (!TryRequest(clientKey, _maxRequests, _windowSeconds))
        {
            await WriteRateLimitResponse(context, 429,
                $"Rate limit exceeded. Max {_maxRequests} requests per {_windowSeconds}s.",
                retryAfterSeconds: _windowSeconds);
            return;
        }

        await _next(context);
    }

    private static string GetClientKey(HttpContext context)
    {
        // Try X-Forwarded-For first (for reverse proxy setups)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var firstIp = forwardedFor.Split(',')[0].Trim();
            if (!string.IsNullOrEmpty(firstIp))
                return $"ip:{firstIp}";
        }

        return $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    private bool TryRequest(string key, int maxRequests, int windowSeconds)
    {
        var now = DateTime.UtcNow;
        var windowSlot = now.Ticks / (windowSeconds * TimeSpan.TicksPerSecond);

        var counter = _clients.GetOrAdd(key, _ => new FixedWindowCounter());

        lock (counter)
        {
            if (counter.WindowSlot != windowSlot)
            {
                counter.WindowSlot = windowSlot;
                counter.Count = 0;
            }

            if (counter.Count >= maxRequests)
                return false;

            counter.Count++;
            return true;
        }
    }

    private void CleanupStaleEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-30);
        var staleKeys = _clients
            .Where(kvp => kvp.Value.LastAccess < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            _clients.TryRemove(key, out _);
        }
    }

    private static async Task WriteRateLimitResponse(HttpContext context, int statusCode, string message, int retryAfterSeconds)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message,
                statusCode,
                retryAfterSeconds,
                timestamp = DateTime.UtcNow,
            }
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private sealed class FixedWindowCounter
    {
        public long WindowSlot { get; set; }
        public int Count { get; set; }
        public DateTime LastAccess { get; set; } = DateTime.UtcNow;
    }
}