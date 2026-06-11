using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class KpiRecord : BaseEntity
{
    public Guid KpiDefinitionId { get; set; }
    public KpiDefinition KpiDefinition { get; set; } = null!;

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? BranchId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ActualValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? TargetValue { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal CompliancePercentage { get; set; }

    public DateOnly EvaluationDate { get; set; }
    public string PeriodKey { get; set; } = string.Empty;
}
