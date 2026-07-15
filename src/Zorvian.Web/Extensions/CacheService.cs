using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Abstraction for caching with TTL support.
/// Implementation auto-selects between Redis and InMemory based on configuration.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task ClearAsync();
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
}

/// <summary>
/// Redis-backed distributed cache using IDistributedCache (StackExchange.Redis).
/// Falls back gracefully if Redis is unavailable.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributed;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultTtl;

    public RedisCacheService(IDistributedCache distributed, ILogger<RedisCacheService> logger, TimeSpan? defaultTtl = null)
    {
        _distributed = distributed;
        _logger = logger;
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(15);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var bytes = await _distributed.GetAsync(key);
            if (bytes is null) return default;
            var json = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}, falling back to null", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? _defaultTtl
            };
            await _distributed.SetAsync(key, bytes, options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributed.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis REMOVE failed for key {Key}", key);
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var bytes = await _distributed.GetAsync(key);
            return bytes is not null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS check failed for key {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync()
    {
        // IDistributedCache doesn't have a global clear — log that it's a no-op
        _logger.LogInformation("Redis CLEAR called — no-op (use Redis FLUSHDB for full clear)");
        await Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached is not null) return cached;
        var value = await factory();
        if (value is not null) await SetAsync(key, value, ttl);
        return value!;
    }
}

/// <summary>
/// In-memory implementation of ICacheService with automatic cleanup.
/// Used as fallback when Redis is not configured.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _defaultTtl;
    private readonly Timer _cleanupTimer;

    public InMemoryCacheService(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromMinutes(15);
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired)
            {
                return Task.FromResult((T?)entry.Value);
            }
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(default(T?));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null)
    {
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? _defaultTtl)
        };
        _cache[key] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired) return Task.FromResult(true);
            _cache.TryRemove(key, out _);
        }
        return Task.FromResult(false);
    }

    public Task ClearAsync()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;
        var value = await factory();
        if (value != null) await SetAsync(key, value, ttl);
        return value!;
    }

    private void CleanupExpired(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt < now)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in expiredKeys)
            _cache.TryRemove(key, out _);
    }

    private class CacheEntry
    {
        public object? Value { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    }
}
