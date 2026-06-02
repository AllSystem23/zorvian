namespace Zorvian.Application.DTOs.Inventory;

public sealed record CreateCategoryRequest(
    string Name,
    string? Description
);

public sealed record UpdateCategoryRequest(
    string? Name,
    string? Description,
    bool? IsActive
);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int ProductCount
);

public sealed record BrandResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    int ProductCount
);

public sealed record CreateBrandRequest(
    string Name,
    string? Description
);

public sealed record UpdateBrandRequest(
    string? Name,
    string? Description,
    bool? IsActive
);

public sealed record SupplierResponse(
    Guid Id,
    string Code,
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxId,
    bool IsActive
);

public sealed record CreateSupplierRequest(
    string Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxId
);

public sealed record UpdateSupplierRequest(
    string? Name,
    string? ContactName,
    string? Phone,
    string? Email,
    string? Address,
    string? TaxId,
    bool? IsActive
);
