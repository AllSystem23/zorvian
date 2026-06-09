using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Services;

public sealed class SyncService : ISyncService
{
    private readonly ZorvianDbContext _db;

    public SyncService(ZorvianDbContext db)
    {
        _db = db;
    }

    public async Task<SyncPullResponse> PullAsync(SyncPullRequest request, string tenantId)
    {
        var since = request.Since ?? DateTime.MinValue;

        var changes = await _db.Set<SyncJournal>()
            .Where(j => j.TenantId == tenantId
                && j.EntityName == request.EntityName
                && j.OccurredAt > since)
            .OrderBy(j => j.OccurredAt)
            .Take(request.Take)
            .Select(j => new SyncChangeDto(
                j.EntityName,
                j.EntityId,
                j.Operation,
                j.PayloadJson,
                j.OccurredAt))
            .ToListAsync();

        var serverTimestamp = DateTime.UtcNow;

        return new SyncPullResponse(changes, serverTimestamp);
    }

    public async Task<SyncPushResponse> PushAsync(List<SyncPushRequest> mutations, string tenantId)
    {
        var conflicts = new List<SyncConflictDto>();

        foreach (var mutation in mutations.OrderBy(m => m.ClientMutationId))
        {
            try
            {
                await JournalAsync(mutation.EntityName, mutation.EntityId, mutation.Operation, mutation.PayloadJson, tenantId);
            }
            catch (Exception ex)
            {
                conflicts.Add(new SyncConflictDto(
                    mutation.ClientMutationId,
                    mutation.EntityName,
                    mutation.EntityId,
                    $"Error: {ex.Message}",
                    mutation.PayloadJson ?? ""));
            }
        }

        return new SyncPushResponse(conflicts, DateTime.UtcNow);
    }

    public async Task JournalAsync(string entityName, string entityId, string operation, string? payloadJson, string tenantId)
    {
        var entry = new SyncJournal
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EntityName = entityName,
            EntityId = entityId,
            Operation = operation,
            PayloadJson = payloadJson,
            OccurredAt = DateTime.UtcNow,
        };

        _db.Set<SyncJournal>().Add(entry);
        await _db.SaveChangesAsync();
    }
}
