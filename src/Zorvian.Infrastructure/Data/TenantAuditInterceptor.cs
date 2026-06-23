using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that automatically populates TenantId, CreatedBy, CreatedAt,
/// UpdatedAt, and UpdatedBy on all entities extending BaseEntity.
/// This prevents the recurring bug where services forget to set these fields.
/// </summary>
public sealed class TenantAuditInterceptor : SaveChangesInterceptor
{
    private readonly ITenantContext _tenant;

    public TenantAuditInterceptor(ITenantContext tenant)
    {
        _tenant = tenant;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var tenantId = _tenant.TenantId?.ToString() ?? string.Empty;
        var userId = _tenant.CurrentUserId?.ToString() ?? string.Empty;
        var now = DateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                        entry.Entity.TenantId = tenantId;
                    if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                        entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
            return base.SavingChanges(eventData, result);

        var tenantId = _tenant.TenantId?.ToString() ?? string.Empty;
        var userId = _tenant.CurrentUserId?.ToString() ?? string.Empty;
        var now = DateTime.UtcNow;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                        entry.Entity.TenantId = tenantId;
                    if (string.IsNullOrEmpty(entry.Entity.CreatedBy))
                        entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return base.SavingChanges(eventData, result);
    }
}
