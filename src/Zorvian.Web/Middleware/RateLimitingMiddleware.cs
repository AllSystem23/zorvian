using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Zorvian.Web.Middleware;

public static class RateLimitingExtensions
{
    public static IApplicationBuilder UseRateLimitingMiddleware(
        this IApplicationBuilder builder,
        int maxRequests = 120,
        int windowSeconds = 60,
        int authLimit = 5,
        int authWindowSeconds = 900,
        int authOtherLimit = 30,
        int authOtherWindowSeconds = 300)
    {
        return builder.UseMiddleware<RateLimitingMiddleware>(
            maxRequests, windowSeconds, authLimit, authWindowSeconds, authOtherLimit, authOtherWindowSeconds);
    }
}

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, FixedWindowCounter> _clients = new();
    private readonly Timer _cleanupTimer;
    private readonly int _maxRequests;
    private readonly int _windowSeconds;
    private readonly int _authLimit;
    private readonly int _authWindowSeconds;
    private readonly int _authOtherLimit;
    private readonly int _authOtherWindowSeconds;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Endpoints that accept credentials. They get a strict per-endpoint budget so a
    /// brute-force run against /auth/login cannot consume the budget of /auth/refresh.
    /// </summary>
    private static readonly string[] CredentialEndpoints =
    [
        "/auth/login",
        "/auth/login-password",
        "/auth/register",
        "/auth/forgot-password",
        "/auth/reset-password",
        "/auth/mfa/login",
        "/auth/mfa/verify",
        "/auth/sso/",
        "/auth/complete-profile",
    ];

    public RateLimitingMiddleware(
        RequestDelegate next,
        int maxRequests = 120,
        int windowSeconds = 60,
        int authLimit = 5,
        int authWindowSeconds = 900,
        int authOtherLimit = 30,
        int authOtherWindowSeconds = 300)
    {
        _next = next;
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;
        _authLimit = authLimit;
        _authWindowSeconds = authWindowSeconds;
        _authOtherLimit = authOtherLimit;
        _authOtherWindowSeconds = authOtherWindowSeconds;

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

        if (path.Contains("/auth/", StringComparison.OrdinalIgnoreCase))
        {
            if (IsCredentialEndpoint(path))
            {
                // Per-endpoint budget: login failures don't lock out /auth/refresh
                var credentialKey = $"auth:{clientKey}:{path}";
                if (!TryRequest(credentialKey, _authLimit, _authWindowSeconds))
                {
                    await WriteRateLimitResponse(context, 429,
                        "Demasiados intentos de autenticación. Intenta de nuevo más tarde.",
                        retryAfterSeconds: _authWindowSeconds);
                    return;
                }
            }
            else if (!TryRequest($"auth-other:{clientKey}", _authOtherLimit, _authOtherWindowSeconds))
            {
                // Refresh / logout / me — automatic calls, so a looser shared budget
                await WriteRateLimitResponse(context, 429,
                    "Demasiadas solicitudes de sesión. Intenta de nuevo más tarde.",
                    retryAfterSeconds: _authOtherWindowSeconds);
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

    private static bool IsCredentialEndpoint(string path)
    {
        foreach (var endpoint in CredentialEndpoints)
        {
            if (path.Contains(endpoint, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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
            counter.LastAccess = now;

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