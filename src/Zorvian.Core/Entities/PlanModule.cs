using Zorvian.Core.Enums;

namespace Zorvian.Core.Entities;

public sealed class PlanModule
{
    public Guid Id { get; set; }
    public string PlanId { get; set; } = string.Empty;
    public ModuleType Module { get; set; }
    public bool IsEnabled { get; set; } = true;
}
