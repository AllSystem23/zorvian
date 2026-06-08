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
    private readonly int _maxRequests;
    private readonly int _windowSeconds;

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 120, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _windowSeconds = windowSeconds;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            if (!TryRequest($"auth:{clientKey}", 5, 900))
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.RetryAfter = "900";
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    error = new { message = "Demasiados intentos de autenticación. Intenta de nuevo en 15 minutos.", statusCode = 429 }
                }));
                return;
            }

            await _next(context);
            return;
        }

        if (!TryRequest(clientKey, _maxRequests, _windowSeconds))
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.RetryAfter = _windowSeconds.ToString();
            context.Response.ContentType = "application/json";

            var error = new
            {
                error = new
                {
                    message = $"Rate limit exceeded. Max {_maxRequests} requests per {_windowSeconds}s. Try again later.",
                    statusCode = 429,
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            return;
        }

        await _next(context);
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

    private sealed class FixedWindowCounter
    {
        public long WindowSlot { get; set; }
        public int Count { get; set; }
    }
}
