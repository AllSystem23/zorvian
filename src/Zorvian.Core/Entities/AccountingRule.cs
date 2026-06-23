namespace Zorvian.Core.Entities;

public sealed class AccountingRule : BaseEntity
{
    public string EventType { get; set; } = string.Empty;
    public string LineType { get; set; } = string.Empty;
    public string AccountRole { get; set; } = string.Empty;
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
