using System.ComponentModel.DataAnnotations.Schema;

namespace Zorvian.Core.Entities;

public sealed class WageGarnishment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string CourtOrder { get; set; } = string.Empty;
    public string GarnishmentType { get; set; } = "percentage"; // 'percentage', 'fixed'
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxPercentage { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string Status { get; set; } = "active"; // 'active', 'completed', 'cancelled'
    public Guid BranchId { get; set; }
}
