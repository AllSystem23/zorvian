using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class CommissionRule : BaseEntity
{
    public Guid CommissionSchemeId { get; set; }
    public CommissionScheme CommissionScheme { get; set; } = null!;

    public int Priority { get; set; }
    public string ConditionType { get; set; } = string.Empty;
    public string ConditionOperator { get; set; } = string.Empty;
    public string ConditionValue { get; set; } = "{}";
    public string CalculationType { get; set; } = "percentage";
    public string CalculationValue { get; set; } = "{}";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Rate { get; set; }

    public string ApplyOn { get; set; } = "gross";
}
