namespace Zorvian.Core.Entities.Fleet;

public sealed class Workshop : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public bool IsInternal { get; set; }
    public bool IsActive { get; set; } = true;
}
