using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class ApiKeyService
{
    private readonly ZorvianDbContext _db;

    public ApiKeyService(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<(string Key, Guid Id)> CreateApiKeyAsync(string name, string tenantId, DateTime? expiresAt = null)
    {
        var rawKey = GenerateSecureKey();
        var prefix = rawKey[..8];
        var hash = HashKey(rawKey);

        var apiKey = new Core.Entities.ApiKey
        {
            Name = name,
            Prefix = prefix,
            KeyHash = hash,
            TenantId = tenantId,
            ExpiresAt = expiresAt,
            IsActive = true
        };

        _db.Set<Core.Entities.ApiKey>().Add(apiKey);
        await _db.SaveChangesAsync();

        return (rawKey, apiKey.Id);
    }

    public async Task<string?> ValidateKeyAsync(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 32) return null;

        var prefix = rawKey[..8];
        var hash = HashKey(rawKey);

        var apiKey = await _db.Set<Core.Entities.ApiKey>()
            .FirstOrDefaultAsync(k => k.Prefix == prefix && k.KeyHash == hash && k.IsActive);

        if (apiKey == null) return null;
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt < DateTime.UtcNow) return null;

        apiKey.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return apiKey.TenantId;
    }

    private static string GenerateSecureKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLower();
    }

    private static string HashKey(string key)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(key);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}
