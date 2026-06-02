namespace Zorvian.Core.Entities;

public sealed class Invitation : BaseEntity
{
    public string Code { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
}
