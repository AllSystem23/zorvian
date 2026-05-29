namespace Nexora.Core.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public string PermissionCode { get; set; } = string.Empty;
}
