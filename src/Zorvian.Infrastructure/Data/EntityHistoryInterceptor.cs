using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

public sealed class EntityHistoryInterceptor : ISaveChangesInterceptor
{
    private static readonly HashSet<string> ExcludedProps =
    [
        "Id", "TenantId", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy",
        "IsDeleted", "DeletedAt"
    ];

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
            return new ValueTask<InterceptionResult<int>>(result);

        var historyEntries = new List<EntityHistory>();

        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            if (entry.Entity is EntityHistory || entry.Entity is AuditLog)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    historyEntries.AddRange(CaptureAdded(entry));
                    break;

                case EntityState.Modified:
                    historyEntries.AddRange(CaptureModified(entry));
                    break;

                case EntityState.Deleted:
                    historyEntries.AddRange(CaptureDeleted(entry));
                    break;
            }
        }

        foreach (var h in historyEntries)
        {
            dbContext.Set<EntityHistory>().Add(h);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    private static List<EntityHistory> CaptureAdded(EntityEntry entry)
    {
        var list = new List<EntityHistory>();
        var entity = (BaseEntity)entry.Entity;
        var entityType = entity.GetType().Name;
        var entityId = entity.Id;

        foreach (var prop in entry.Properties)
        {
            if (ExcludedProps.Contains(prop.Metadata.Name))
                continue;

            list.Add(new EntityHistory
            {
                EntityType = entityType,
                EntityId = entityId,
                ChangeType = "Create",
                FieldName = prop.Metadata.Name,
                NewValue = prop.CurrentValue?.ToString(),
            });
        }

        return list;
    }

    private static List<EntityHistory> CaptureModified(EntityEntry entry)
    {
        var list = new List<EntityHistory>();
        var entity = (BaseEntity)entry.Entity;
        var entityType = entity.GetType().Name;
        var entityId = entity.Id;

        foreach (var prop in entry.Properties)
        {
            if (ExcludedProps.Contains(prop.Metadata.Name))
                continue;

            if (!prop.IsModified)
                continue;

            var oldVal = prop.OriginalValue?.ToString();
            var newVal = prop.CurrentValue?.ToString();

            if (Equals(oldVal, newVal))
                continue;

            list.Add(new EntityHistory
            {
                EntityType = entityType,
                EntityId = entityId,
                ChangeType = "Update",
                FieldName = prop.Metadata.Name,
                OldValue = oldVal,
                NewValue = newVal,
            });
        }

        return list;
    }

    private static List<EntityHistory> CaptureDeleted(EntityEntry entry)
    {
        var entity = (BaseEntity)entry.Entity;
        return
        [
            new EntityHistory
            {
                EntityType = entity.GetType().Name,
                EntityId = entity.Id,
                ChangeType = "Delete",
                FieldName = "*",
                OldValue = "Deleted",
            }
        ];
    }
}
