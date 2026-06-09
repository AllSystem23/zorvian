using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zorvian.Web.Filters;

/// <summary>
/// Attribute that ensures idempotency for write operations (POST/PUT/PATCH).
/// Uses an in-memory cache with TTL. Clients must send X-Idempotency-Key header.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class IdempotentAttribute : Attribute, IAsyncActionFilter
{
    private static readonly ConcurrentDictionary<string, CachedResponse> _cache = new();
    private readonly int _ttlMinutes;

    public IdempotentAttribute(int ttlMinutes = 60)
    {
        _ttlMinutes = ttlMinutes;

        // Cleanup timer
        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromMinutes(5));
                var cutoff = DateTime.UtcNow.AddMinutes(-ttlMinutes);
                var stale = _cache.Where(kvp => kvp.Value.Timestamp < cutoff).Select(kvp => kvp.Key).ToList();
                foreach (var key in stale)
                    _cache.TryRemove(key, out _);
            }
        });
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var idempotencyKey = context.HttpContext.Request.Headers["X-Idempotency-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = new { message = "X-Idempotency-Key header is required for write operations.", statusCode = 400 }
            });
            return;
        }

        if (_cache.TryGetValue(idempotencyKey, out var cached))
        {
            // Return cached response
            context.Result = new ObjectResult(cached.Body)
            {
                StatusCode = cached.StatusCode,
            };
            context.HttpContext.Response.Headers["X-Idempotent-Replayed"] = "true";
            return;
        }

        // Execute the action
        var resultContext = await next();

        // Cache the response
        if (resultContext.Result is ObjectResult objectResult)
        {
            _cache[idempotencyKey] = new CachedResponse
            {
                StatusCode = objectResult.StatusCode ?? 200,
                Body = objectResult.Value,
                Timestamp = DateTime.UtcNow,
            };
        }
    }

    private sealed class CachedResponse
    {
        public int StatusCode { get; set; }
        public object? Body { get; set; }
        public DateTime Timestamp { get; set; }
    }
}