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

    public object CreatedInstance(MaterializationInterceptionData data, object entity)
    {
        ApplyToEncryptedProperties(entity, _encryptionService.Decrypt);
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
