namespace Zorvian.Core.Entities;

public sealed class ProviderContact : BaseEntity
{
    public Guid ProviderId { get; set; }
    public WarrantyProvider Provider { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}
