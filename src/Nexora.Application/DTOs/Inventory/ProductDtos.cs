namespace Nexora.Application.DTOs.Inventory;

public sealed record CreateProductRequest(
    string Code,
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? SupplierId,
    decimal CostPrice,
    decimal SellingPrice,
    string UnitOfMeasure,
    int Stock,
    int MinStock,
    int MaxStock,
    string? Location,
    string? Barcode,
    Guid BranchId
);

public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? SupplierId,
    decimal? CostPrice,
    decimal? SellingPrice,
    string? UnitOfMeasure,
    int? MinStock,
    int? MaxStock,
    string? Location,
    string? Barcode,
    bool? IsActive
);

public sealed record ProductResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    Guid? BrandId,
    string? BrandName,
    Guid? SupplierId,
    string? SupplierName,
    decimal CostPrice,
    decimal SellingPrice,
    string UnitOfMeasure,
    int Stock,
    int MinStock,
    int MaxStock,
    string? Location,
    string? Barcode,
    bool IsActive,
    Guid BranchId
);

public sealed record ProductListResponse(
    Guid Id,
    string Code,
    string Name,
    string? CategoryName,
    decimal SellingPrice,
    int Stock,
    int MinStock,
    bool IsActive
);

public sealed record ProductFilterRequest(
    string? Search,
    Guid? CategoryId,
    Guid? BrandId,
    bool? LowStock,
    bool? IsActive,
    int? Page = 1,
    int? PageSize = 20
);
