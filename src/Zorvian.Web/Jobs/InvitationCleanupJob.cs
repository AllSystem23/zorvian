using Microsoft.EntityFrameworkCore;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Web.Jobs;

public sealed class InvitationCleanupJob
{
    private readonly ZorvianDbContext _db;
    private readonly ILogger<InvitationCleanupJob> _logger;

    public InvitationCleanupJob(ZorvianDbContext db, ILogger<InvitationCleanupJob> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation("Starting invitation cleanup. Removing expired invitations as of {Now}", now);

        var expired = await _db.Invitations
            .IgnoreQueryFilters()
            .Where(i => i.ExpiresAt != null && i.ExpiresAt < now && !i.IsDeleted)
            .ToListAsync();

        if (expired.Count == 0)
        {
            _logger.LogInformation("No expired invitations to clean up");
            return;
        }

        // Soft-delete: set IsDeleted = true and DeletedAt = now
        foreach (var invitation in expired)
        {
            invitation.IsDeleted = true;
            invitation.DeletedAt = now;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Soft-deleted {Count} expired invitation(s)", expired.Count);
    }
}
