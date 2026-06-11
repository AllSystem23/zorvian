using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class KpiDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string KpiCategory { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public string Frequency { get; set; } = "monthly";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetValue { get; set; }

    public string Unit { get; set; } = "%";
    public string VisualizationType { get; set; } = "number";
    public bool IsActive { get; set; } = true;

    public ICollection<KpiRecord> Records { get; set; } = [];
}
