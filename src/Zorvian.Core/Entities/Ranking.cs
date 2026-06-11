using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class Ranking : BaseEntity
{
    public string RankingType { get; set; } = string.Empty;
    public string PeriodKey { get; set; } = string.Empty;
    public int Position { get; set; }
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? Growth { get; set; }

    public Guid? BranchId { get; set; }
}
