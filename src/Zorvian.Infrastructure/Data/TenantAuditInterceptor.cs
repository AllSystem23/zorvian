using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<TenantAuditInterceptor> _logger;

    public TenantAuditInterceptor(ITenantContext tenant, ILogger<TenantAuditInterceptor> logger)
    {
        _tenant = tenant;
        _logger = logger;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            ApplyAudit(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void ApplyAudit(DbContext context)
    {
        var tenantId = _tenant.TenantId?.ToString() ?? string.Empty;
        var userId = _tenant.CurrentUserId?.ToString() ?? string.Empty;
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                    {
                        if (string.IsNullOrEmpty(tenantId))
                            _logger.LogWarning(
                                "TenantAuditInterceptor: TenantId is empty for {EntityType}#{EntityId}. " +
                                "ITenantContext.TenantId was not configured before SaveChanges.",
                                entry.Entity.GetType().Name, entry.Entity.Id);

                        entry.Entity.TenantId = tenantId;
                    }

                    if (entry.Entity.CompanyId == Guid.Empty)
                    {
                        if (Guid.TryParse(tenantId, out var companyId))
                        {
                            entry.Entity.CompanyId = companyId;
                        }
                        else if (!string.IsNullOrEmpty(tenantId))
                        {
                            _logger.LogWarning(
                                "TenantAuditInterceptor: Could not parse TenantId '{TenantId}' to Guid for {EntityType}#{EntityId}.",
                                tenantId, entry.Entity.GetType().Name, entry.Entity.Id);
                        }
                    }

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
    }
}
