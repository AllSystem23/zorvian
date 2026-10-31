using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

public sealed class AuditInterceptor : ISaveChangesInterceptor
{
    /// <summary>
    /// Tope por valor serializado. El snapshot de una entidad guarda sus propiedades tal
    /// cual, y entidades como <c>DocumentVersion</c>/<c>DocumentTemplate</c> llevan el HTML
    /// completo (40-80 KB): un solo "Create" inflaba el log y contribuyó a llenar la base
    /// (incidente de Neon, 1024 MB). Se recorta el VALOR, no el JSON, para que la fila
    /// siga siendo parseable.
    /// </summary>
    public const int MaxSerializedValueLength = 1_000;

    private const string TruncatedSuffix = "...[truncado]";

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
                            oldDict[prop.Metadata.Name] = ClampValue(prop.OriginalValue);
                            newDict[prop.Metadata.Name] = ClampValue(prop.CurrentValue);
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
            dict[prop.Metadata.Name] = ClampValue(prop.CurrentValue);
        }
        return JsonSerializer.Serialize(dict, JsonOptions);
    }

    /// <summary>
    /// Recorta valores gigantes (HTML de plantillas/documentos, payloads de webhooks) para
    /// que una sola fila de auditoría no pese decenas de KB. Los valores normales pasan
    /// intactos.
    /// </summary>
    private static object? ClampValue(object? value)
    {
        if (value is not string text || text.Length <= MaxSerializedValueLength)
            return value;

        return string.Concat(
            text.AsSpan(0, MaxSerializedValueLength),
            TruncatedSuffix);
    }
}
