using Microsoft.EntityFrameworkCore;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Extensions;

/// <summary>
/// Helper for batch operations on the database
/// </summary>
public static class BatchOperationHelper
{
    /// <summary>
    /// Soft delete multiple entities in a single operation
    /// </summary>
    public static async Task<int> SoftDeleteBatchAsync<T>(this ZorvianDbContext context, IEnumerable<Guid> ids, Guid userId) where T : BaseEntity
    {
        var entities = await context.Set<T>().Where(e => ids.Contains(e.Id)).ToListAsync();
        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = userId.ToString();
        }
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Activate or deactivate multiple entities in a single operation
    /// </summary>
    public static async Task<int> ToggleActiveBatchAsync<T>(this ZorvianDbContext context, IEnumerable<Guid> ids, bool isActive, Guid userId) where T : BaseEntity
    {
        var entities = await context.Set<T>().Where(e => ids.Contains(e.Id)).ToListAsync();
        foreach (var entity in entities)
        {
            if (entity is BaseEntity be)
            {
                be.UpdatedAt = DateTime.UtcNow;
                be.UpdatedBy = userId.ToString();
            }
        }
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Execute a batch update on multiple records
    /// </summary>
    public static async Task<int> ExecuteBatchUpdateAsync<T>(this ZorvianDbContext context, IEnumerable<Guid> ids, Action<T> updateAction) where T : BaseEntity
    {
        var entities = await context.Set<T>().Where(e => ids.Contains(e.Id)).ToListAsync();
        foreach (var entity in entities)
        {
            updateAction(entity);
            entity.UpdatedAt = DateTime.UtcNow;
        }
        return await context.SaveChangesAsync();
    }
}
