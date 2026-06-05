using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

public sealed class AuditLogCleanupJob
{
    private readonly ZorvianDbContext _db;
    private readonly ILogger<AuditLogCleanupJob> _logger;

    public AuditLogCleanupJob(ZorvianDbContext db, ILogger<AuditLogCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var cutoff = DateTime.UtcNow.AddMonths(-6);
        _logger.LogInformation("Starting audit log cleanup. Removing logs older than {Cutoff}", cutoff);

        var oldLogs = await _db.AuditLogs
            .IgnoreQueryFilters()
            .Where(a => a.CreatedAt < cutoff)
            .ToListAsync();

        if (oldLogs.Count == 0)
        {
            _logger.LogInformation("No audit logs to clean up");
            return;
        }

        _db.AuditLogs.RemoveRange(oldLogs);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted {Count} old audit log entries", oldLogs.Count);
    }
}
