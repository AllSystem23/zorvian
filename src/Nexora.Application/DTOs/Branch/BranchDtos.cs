namespace Nexora.Application.DTOs.Branch;

public sealed record CreateBranchRequest(
    string Name,
    string? Code,
    string? Address,
    string? Phone,
    string? Email
);

public sealed record UpdateBranchRequest(
    string? Name,
    string? Code,
    string? Address,
    string? Phone,
    string? Email,
    bool? IsActive
);

public sealed record BranchResponse(
    Guid Id,
    string Name,
    string? Code,
    string? Address,
    string? Phone,
    string? Email,
    bool IsActive,
    Guid CompanyId
);

public sealed record BranchFilterRequest(
    string? Search,
    bool? IsActive,
    int? Page = 1,
    int? PageSize = 20
);
