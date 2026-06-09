namespace Zorvian.Application.Interfaces;

public sealed record SyncPullRequest(string EntityName, DateTime? Since, int Take = 500);

public sealed record SyncPullResponse(
    List<SyncChangeDto> Changes,
    DateTime ServerTimestamp);

public sealed record SyncChangeDto(
    string EntityName,
    string EntityId,
    string Operation,
    string? PayloadJson,
    DateTime OccurredAt);

public sealed record SyncPushRequest(
    string EntityName,
    string EntityId,
    string Operation,
    string? PayloadJson,
    string ClientMutationId);

public sealed record SyncPushResponse(
    List<SyncConflictDto> Conflicts,
    DateTime ServerTimestamp);

public sealed record SyncConflictDto(
    string ClientMutationId,
    string EntityName,
    string EntityId,
    string ServerPayloadJson,
    string ClientPayloadJson);

public interface ISyncService
{
    Task<SyncPullResponse> PullAsync(SyncPullRequest request, string tenantId);
    Task<SyncPushResponse> PushAsync(List<SyncPushRequest> mutations, string tenantId);
    Task JournalAsync(string entityName, string entityId, string operation, string? payloadJson, string tenantId);
}
