using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class Incentive : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string IncentiveType { get; set; } = "bonus";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    public string Currency { get; set; } = "NIO";
    public string PaymentTrigger { get; set; } = "automatic";
    public string Status { get; set; } = "active";
    public Guid? GoalDefinitionId { get; set; }
    public GoalDefinition? GoalDefinition { get; set; }
}
