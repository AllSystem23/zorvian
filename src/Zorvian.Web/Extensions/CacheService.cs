using System.Collections.Concurrent;

namespace Zorvian.Web.Extensions;

/// <summary>
/// In-memory cache service with TTL support.
/// For production, replace with Redis (IDistributedCache).
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
/// In-memory implementation of ICacheService with automatic cleanup
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
