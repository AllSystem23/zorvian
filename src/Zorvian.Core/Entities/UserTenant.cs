namespace Zorvian.Core.Entities;

public sealed class UserTenant
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TenantId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
