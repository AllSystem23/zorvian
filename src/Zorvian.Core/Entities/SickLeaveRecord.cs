namespace Zorvian.Core.Entities;

public sealed class SickLeaveRecord : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? DiagnosisCode { get; set; }
    public string? CertificateUrl { get; set; }
    public decimal EmployerCoverage { get; set; }
    public decimal InssCoverage { get; set; }
    public string Status { get; set; } = "pending"; // pending, approved, reimbursed
}
