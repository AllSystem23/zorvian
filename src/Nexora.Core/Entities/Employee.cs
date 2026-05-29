namespace Nexora.Core.Entities;

public sealed class Employee : BaseEntity
{
    public string? EmployeeCode { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? IdentificationType { get; set; }
    public string? IdentificationNumber { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string? Position { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public decimal? Salary { get; set; }
    public string? SalaryType { get; set; } = "monthly";
    public string Status { get; set; } = "active";
    public string? PhotoUrl { get; set; }
}
