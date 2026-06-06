using Zorvian.Core.Entities;

namespace Zorvian.Application.Services;

public static class EntityHistoryHelper
{
    public static List<EntityHistory> CaptureChanges<T>(
        string entityType,
        Guid entityId,
        string changeType,
        Dictionary<string, object?> before,
        T after)
    {
        var entries = new List<EntityHistory>();

        foreach (var kvp in before)
        {
            var oldVal = kvp.Value;
            var newVal = typeof(T).GetProperty(kvp.Key)?.GetValue(after);

            if (!Equals(oldVal, newVal))
            {
                entries.Add(new EntityHistory
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    ChangeType = changeType,
                    FieldName = kvp.Key,
                    OldValue = oldVal?.ToString(),
                    NewValue = newVal?.ToString(),
                });
            }
        }

        return entries;
    }

    public static List<EntityHistory> CreateEntry(
        string entityType,
        Guid entityId,
        string changeType,
        string fieldName,
        string? newValue,
        string? oldValue = null)
    {
        return
        [
            new EntityHistory
            {
                EntityType = entityType,
                EntityId = entityId,
                ChangeType = changeType,
                FieldName = fieldName,
                OldValue = oldValue,
                NewValue = newValue,
            }
        ];
    }
}
