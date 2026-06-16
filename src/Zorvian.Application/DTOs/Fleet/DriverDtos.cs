namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateDriverRequest(
    Guid? EmployeeId,
    string FirstName,
    string LastName,
    string IdDocument,
    DateOnly BirthDate,
    string Phone,
    string Email,
    string? Address,
    Guid LicenseCategoryId,
    string LicenseNumber,
    DateOnly LicenseIssueDate,
    DateOnly LicenseExpiryDate,
    string? AdditionalCategories,
    DateOnly HireDate,
    Guid BranchId,
    string? PhotoUrl
);

public sealed record UpdateDriverRequest(
    Guid? EmployeeId,
    string? FirstName,
    string? LastName,
    string? IdDocument,
    DateOnly? BirthDate,
    string? Phone,
    string? Email,
    string? Address,
    Guid? LicenseCategoryId,
    string? LicenseNumber,
    DateOnly? LicenseIssueDate,
    DateOnly? LicenseExpiryDate,
    string? AdditionalCategories,
    string? Status,
    Guid? BranchId,
    string? PhotoUrl
);

public sealed record DriverResponse(
    Guid Id,
    Guid? EmployeeId,
    string FirstName,
    string LastName,
    string FullName,
    string IdDocument,
    DateOnly BirthDate,
    string Phone,
    string Email,
    string? Address,
    Guid LicenseCategoryId,
    string LicenseCategoryName,
    string LicenseNumber,
    DateOnly LicenseIssueDate,
    DateOnly LicenseExpiryDate,
    string? AdditionalCategories,
    DateOnly HireDate,
    string Status,
    Guid BranchId,
    string BranchName,
    string? PhotoUrl,
    DateTime CreatedAt
);
