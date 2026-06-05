namespace Zorvian.Core.Entities;

public sealed class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid CompanyId { get; set; }
}
