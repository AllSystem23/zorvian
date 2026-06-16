namespace Zorvian.Core.Entities.Fleet;

public sealed class Driver : BaseEntity
{
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    public string IdDocument { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Address { get; set; }
    public Guid LicenseCategoryId { get; set; }
    public DriverLicenseCategory LicenseCategory { get; set; } = null!;
    public string LicenseNumber { get; set; } = string.Empty;
    public DateOnly LicenseIssueDate { get; set; }
    public DateOnly LicenseExpiryDate { get; set; }
    public string? AdditionalCategories { get; set; }
    public DateOnly HireDate { get; set; }
    public string Status { get; set; } = "Active";
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public string? PhotoUrl { get; set; }
}
