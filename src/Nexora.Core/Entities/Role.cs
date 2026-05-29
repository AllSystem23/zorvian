using Nexora.Core.Enums;

namespace Nexora.Core.Entities;

public sealed class Role : BaseEntity
{
    public RoleType Name { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
