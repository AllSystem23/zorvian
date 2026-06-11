using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class CommissionAssignment : BaseEntity
{
    public Guid CommissionSchemeId { get; set; }
    public CommissionScheme CommissionScheme { get; set; } = null!;

    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly EffectiveDate { get; set; }
    public DateOnly? ExpirationDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? TeamPercentage { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<CommissionRecord> GeneratedRecords { get; set; } = [];
}
