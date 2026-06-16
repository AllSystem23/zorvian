namespace Zorvian.Core.Entities.Fleet;

public sealed class DocumentType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public bool HasExpiry { get; set; }
    public int AlertDaysBefore { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
}
