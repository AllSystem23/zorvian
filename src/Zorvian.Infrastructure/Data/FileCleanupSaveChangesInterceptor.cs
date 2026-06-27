using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;

namespace Zorvian.Infrastructure.Data;

/// <summary>
/// EF Core interceptor that captures file URLs from entities being soft-deleted
/// (IsDeleted = true) and schedules their cleanup from storage after SaveChanges.
/// Must be registered BEFORE AuditInterceptor so it captures URLs before state changes.
/// </summary>
public sealed class FileCleanupSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IOrphanFileCleanupService _cleanupService;
    private readonly List<string> _pendingUrls = new();

    public FileCleanupSaveChangesInterceptor(IOrphanFileCleanupService cleanupService)
    {
        _cleanupService = cleanupService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
            return new ValueTask<InterceptionResult<int>>(result);

        // Capture file URLs from entities about to be deleted (soft or hard)
        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State != EntityState.Deleted) continue;
            if (entry.Entity is AuditLog) continue;

            var urls = _cleanupService.ExtractFileUrls(entry.Entity);
            _pendingUrls.AddRange(urls);
        }

        return new ValueTask<InterceptionResult<int>>(result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingUrls.Count > 0)
        {
            var urlsToClean = new List<string>(_pendingUrls);
            _pendingUrls.Clear();

            // Fire-and-forget: clean up files in background so SaveChanges isn't blocked
            _ = Task.Run(async () =>
            {
                try
                {
                    await _cleanupService.CleanupFilesAsync(urlsToClean, cancellationToken);
                }
                catch
                {
                    // Logged inside CleanupFilesAsync
                }
            }, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
