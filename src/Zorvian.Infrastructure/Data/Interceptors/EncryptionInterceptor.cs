using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Attributes;

namespace Zorvian.Infrastructure.Data.Interceptors;

public sealed class EncryptionInterceptor : ISaveChangesInterceptor, IMaterializationInterceptor
{
    private readonly IEncryptionService _encryptionService;

    public EncryptionInterceptor(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    // ── Decryption on Load ──
    // IMPORTANT: decryption MUST run in InitializedInstance, which EF invokes
    // AFTER setting the property values from the database. CreatedInstance runs
    // BEFORE properties are set, so decrypting there is immediately overwritten
    // with the raw ciphertext (this caused encrypted PII to leak to the API).

    public object InitializedInstance(MaterializationInterceptionData data, object entity)
    {
        ApplyToEncryptedProperties(entity, DecryptSafe);
        return entity;
    }

    // ── Encryption on Save ──

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null) return new ValueTask<InterceptionResult<int>>(result);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            ApplyToEncryptedProperties(entry.Entity, _encryptionService.Encrypt);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private string DecryptSafe(string value)
    {
        try
        {
            return _encryptionService.Decrypt(value);
        }
        catch
        {
            // Value is not valid ciphertext (legacy plaintext or corrupt data).
            // Return as-is instead of failing every read of the entity.
            return value;
        }
    }

    private static void ApplyToEncryptedProperties(object entity, Func<string, string> transform)
    {
        var properties = entity.GetType().GetProperties()
            .Where(p => p.GetCustomAttribute<EncryptedAttribute>() != null && p.PropertyType == typeof(string));

        foreach (var prop in properties)
        {
            var value = prop.GetValue(entity) as string;
            if (string.IsNullOrEmpty(value)) continue;
            prop.SetValue(entity, transform(value));
        }
    }
}
