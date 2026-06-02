namespace Zorvian.Core.Entities;

public sealed class BiometricRegistration : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastVerifiedAt { get; set; }
}
