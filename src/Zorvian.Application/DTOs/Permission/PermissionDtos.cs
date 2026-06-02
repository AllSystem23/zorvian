namespace Zorvian.Application.DTOs.Permission;

public sealed record PermissionTypeResponse(
    Guid Id,
    string Code,
    string Name,
    bool IsPaid,
    bool RequiresAttachment,
    bool RequiresApproval,
    int? MaxDaysPerRequest,
    int? MaxDaysPerMonth,
    int? MaxDaysPerYear,
    string? Description
);

public sealed record CreatePermissionRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason,
    string? SupportingDocumentUrl,
    string? SupportingDocumentFileName
);

public sealed record PermissionResponse(
    Guid Id,
    Guid EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    decimal BusinessDays,
    string? Reason,
    string Status,
    string? SupportingDocumentUrl,
    string? SupportingDocumentFileName,
    string? RejectionReason,
    bool IsPaid,
    DateTime CreatedAt
);

public sealed record PermissionFilterRequest(
    string? Status,
    Guid? EmployeeId,
    Guid? LeaveTypeId,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record CreateLeaveTypeRequest(
    string Code,
    string Name,
    string? Description,
    bool IsPaid = false,
    bool RequiresAttachment = false,
    bool RequiresApproval = true,
    int? MaxDaysPerRequest = null,
    int? MaxDaysPerMonth = null,
    int? MaxDaysPerYear = null,
    string? Country = "NI"
);
