namespace Zorvian.Core.Entities;

public sealed class SyncJournal : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? PayloadJson { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
