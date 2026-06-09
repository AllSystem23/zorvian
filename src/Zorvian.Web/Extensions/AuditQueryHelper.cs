using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Helper for querying audit logs efficiently
/// </summary>
public static class AuditQueryHelper
{
    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    public static async Task<List<AuditLog>> GetEntityHistoryAsync(
        this ZorvianDbContext context,
        string entityType,
        Guid entityId,
        int page = 1,
        int pageSize = 50)
    {
        return await context.Set<AuditLog>()
            .Where(a => a.EntityName == entityType && a.EntityId == entityId.ToString())
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Get all actions by a user within a date range
    /// </summary>
    public static async Task<List<AuditLog>> GetUserActivityAsync(
        this ZorvianDbContext context,
        Guid userId,
        DateTime from,
        DateTime to)
    {
        return await context.Set<AuditLog>()
            .Where(a => a.PerformedBy == userId && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(500)
            .ToListAsync();
    }

    /// <summary>
    /// Get critical actions (delete, hard delete) for monitoring
    /// </summary>
    public static async Task<List<AuditLog>> GetCriticalActionsAsync(
        this ZorvianDbContext context,
        DateTime from,
        DateTime to)
    {
        var criticalActions = new[] { "Delete", "HardDelete", "BulkDelete", "RoleChange", "PermissionChange" };
        return await context.Set<AuditLog>()
            .Where(a => criticalActions.Contains(a.Action) && a.CreatedAt >= from && a.CreatedAt <= to)
            .OrderByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync();
    }

    /// <summary>
    /// Count actions by type for analytics
    /// </summary>
    public static async Task<Dictionary<string, int>> GetActionStatisticsAsync(
        this ZorvianDbContext context,
        DateTime from,
        DateTime to)
    {
        return await context.Set<AuditLog>()
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
            .GroupBy(a => a.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);
    }
}
