using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class AuditInterceptor : ISaveChangesInterceptor
{
    private readonly ITenantContext _tenantContext;

    public AuditInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
            return new ValueTask<InterceptionResult<int>>(result);

        var auditEntries = new List<AuditLog>();
        var now = DateTime.UtcNow;
        var currentUserId = _tenantContext.CurrentUserId;

        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            if (entry.Entity is AuditLog)
                continue;

            string action;
            string? oldValues = null;
            string? newValues = null;
            string? changedProperties = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Create";
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = _tenantContext.CurrentUserId?.ToString() ?? "system";
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                        entry.Entity.TenantId = _tenantContext.TenantId;
                    newValues = SerializeEntry(entry);
                    break;

                case EntityState.Modified:
                    action = "Update";
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = _tenantContext.CurrentUserId?.ToString();

                    var oldDict = new Dictionary<string, object?>();
                    var newDict = new Dictionary<string, object?>();
                    var changedList = new List<string>();

                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified && prop.Metadata.Name is not ("UpdatedAt" or "UpdatedBy"))
                        {
                            oldDict[prop.Metadata.Name] = prop.OriginalValue;
                            newDict[prop.Metadata.Name] = prop.CurrentValue;
                            changedList.Add(prop.Metadata.Name);
                        }
                    }

                    if (changedList.Count > 0)
                    {
                        oldValues = JsonSerializer.Serialize(oldDict, JsonOptions);
                        newValues = JsonSerializer.Serialize(newDict, JsonOptions);
                        changedProperties = string.Join(",", changedList);
                    }
                    break;

                case EntityState.Deleted:
                    action = "Delete";
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = now;
                    oldValues = SerializeEntry(entry);
                    break;

                default:
                    continue;
            }

            var entityName = entry.Entity.GetType().Name;
            var entityId = entry.Entity.Id.ToString();

            var log = new AuditLog
            {
                TenantId = _tenantContext.TenantId,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                ChangedProperties = changedProperties,
                PerformedBy = currentUserId,
            };

            auditEntries.Add(log);
        }

        foreach (var log in auditEntries)
        {
            dbContext.Set<AuditLog>().Add(log);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string? SerializeEntry(EntityEntry entry)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.Name is "CreatedAt" or "CreatedBy" or "UpdatedAt" or "UpdatedBy" or "IsDeleted" or "DeletedAt")
                continue;
            dict[prop.Metadata.Name] = prop.CurrentValue;
        }
        return JsonSerializer.Serialize(dict, JsonOptions);
    }
}
