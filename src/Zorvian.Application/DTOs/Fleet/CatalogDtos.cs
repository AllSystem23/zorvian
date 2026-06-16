namespace Zorvian.Application.DTOs.Fleet;

public sealed record CreateMaintenanceTemplateRequest(
    string Name,
    string? Description,
    string? ApplicableVehicleTypes,
    int? DefaultIntervalKm,
    int? DefaultIntervalDays,
    int? DefaultIntervalHours
);

public sealed record UpdateMaintenanceTemplateRequest(
    string? Name,
    string? Description,
    string? ApplicableVehicleTypes,
    int? DefaultIntervalKm,
    int? DefaultIntervalDays,
    int? DefaultIntervalHours,
    bool? IsActive
);

public sealed record MaintenanceTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ApplicableVehicleTypes,
    int? DefaultIntervalKm,
    int? DefaultIntervalDays,
    int? DefaultIntervalHours,
    bool IsActive
);

public sealed record CreateFailureTypeRequest(string Name, string? Description);
public sealed record UpdateFailureTypeRequest(string? Name, string? Description, bool? IsActive);
public sealed record FailureTypeResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record CreateWorkshopRequest(
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    bool IsInternal
);

public sealed record UpdateWorkshopRequest(
    string? Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    bool? IsInternal,
    bool? IsActive
);

public sealed record WorkshopResponse(
    Guid Id,
    string Name,
    string? ContactPerson,
    string Phone,
    string? Email,
    string? Address,
    bool IsInternal,
    bool IsActive
);

public sealed record CreateDocumentTypeRequest(
    string Name,
    string EntityType,
    bool HasExpiry,
    int AlertDaysBefore,
    bool IsRequired
);

public sealed record UpdateDocumentTypeRequest(
    string? Name,
    string? EntityType,
    bool? HasExpiry,
    int? AlertDaysBefore,
    bool? IsRequired,
    bool? IsActive
);

public sealed record DocumentTypeResponse(
    Guid Id,
    string Name,
    string EntityType,
    bool HasExpiry,
    int AlertDaysBefore,
    bool IsRequired,
    bool IsActive
);

public sealed record CreateExpenseCategoryRequest(string Name, string? Description);
public sealed record UpdateExpenseCategoryRequest(string? Name, string? Description, bool? IsActive);
public sealed record ExpenseCategoryResponse(Guid Id, string Name, string? Description, bool IsActive);

public sealed record CreateExpenseSubcategoryRequest(Guid CategoryId, string Name);
public sealed record UpdateExpenseSubcategoryRequest(Guid? CategoryId, string? Name, bool? IsActive);
public sealed record ExpenseSubcategoryResponse(Guid Id, Guid CategoryId, string CategoryName, string Name, bool IsActive);
