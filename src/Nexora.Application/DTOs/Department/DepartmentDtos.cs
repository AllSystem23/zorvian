namespace Nexora.Application.DTOs.Department;

public sealed record CreateDepartmentRequest(
    string Name,
    string? Code,
    string? Description,
    Guid? ManagerId,
    Guid? ParentDepartmentId
);

public sealed record UpdateDepartmentRequest(
    string? Name,
    string? Code,
    string? Description,
    Guid? ManagerId,
    Guid? ParentDepartmentId,
    bool? IsActive
);

public sealed record DepartmentResponse(
    Guid Id,
    string Name,
    string Code,
    string Description,
    string ManagerName,
    Guid? ParentDepartmentId,
    bool IsActive,
    int EmployeeCount
);
