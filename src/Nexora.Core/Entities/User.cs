namespace Nexora.Core.Entities;

public sealed class User : BaseEntity
{
    public string FirebaseUid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
