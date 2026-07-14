namespace Zorvian.Application.Messages;

/// <summary>
/// Event published when an employee is created or updated.
/// Consumed by: Payroll, Attendance, Benefits, BI
/// </summary>
public sealed record EmployeeCreatedEvent
{
    public Guid EmployeeId { get; init; }
    public Guid CompanyId { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string Position { get; init; } = string.Empty;
    public decimal Salary { get; init; }
    public DateTime HireDate { get; init; }
    public string CountryCode { get; init; } = string.Empty;
}
