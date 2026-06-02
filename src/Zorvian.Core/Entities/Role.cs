using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

public sealed class Role : BaseEntity
{
    public RoleType Name { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
// Note: Role already inherits from BaseEntity, which has TenantId.
// The issue is likely that Role is treated as global in some places and tenant-specific in others.
// To fix the CS10622 warning, I need to add the filter to the DbContext definition for Role.
