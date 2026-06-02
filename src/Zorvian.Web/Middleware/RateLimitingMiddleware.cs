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
    private readonly ConcurrentDictionary<string, SlidingWindow> _clients = new();
    private readonly int _maxRequests;
    private readonly TimeSpan _windowSize;

    public RateLimitingMiddleware(RequestDelegate next, int maxRequests = 120, int windowSeconds = 60)
    {
        _next = next;
        _maxRequests = maxRequests;
        _windowSize = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var window = _clients.GetOrAdd(clientKey, _ => new SlidingWindow(_maxRequests, _windowSize));

        if (!window.TryRequest())
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.RetryAfter = ((int)_windowSize.TotalSeconds).ToString();
            context.Response.ContentType = "application/json";

            var error = new
            {
                error = new
                {
                    message = $"Rate limit exceeded. Max {_maxRequests} requests per {_windowSize.TotalSeconds}s. Try again later.",
                    statusCode = 429,
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            return;
        }

        await _next(context);
    }

    private sealed class SlidingWindow
    {
        private readonly int _maxRequests;
        private readonly TimeSpan _windowSize;
        private readonly ConcurrentQueue<DateTime> _timestamps = new();
        private readonly object _lock = new();

        public SlidingWindow(int maxRequests, TimeSpan windowSize)
        {
            _maxRequests = maxRequests;
            _windowSize = windowSize;
        }

        public bool TryRequest()
        {
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                while (_timestamps.TryPeek(out var oldest) && now - oldest > _windowSize)
                    _timestamps.TryDequeue(out _);

                if (_timestamps.Count >= _maxRequests)
                    return false;

                _timestamps.Enqueue(now);
                return true;
            }
        }
    }
}
