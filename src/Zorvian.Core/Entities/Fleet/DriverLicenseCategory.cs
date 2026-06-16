namespace Zorvian.Core.Entities.Fleet;

public sealed class DriverLicenseCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
