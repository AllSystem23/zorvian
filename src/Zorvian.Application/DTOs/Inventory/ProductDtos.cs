namespace Zorvian.Application.DTOs.Inventory;

public sealed record CreateProductRequest(
    string Code,
    string Name,
    string? Description,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? SupplierId,
    Guid? TaxCategoryId,
    decimal CostPrice,
    decimal SellingPrice,
    string UnitOfMeasure,
    int Stock,
    int MinStock,
    int MaxStock,
    string? Location,
    string? Barcode,
    Guid BranchId,
    Guid? WarehouseId   // null = bodega por defecto/contexto de la sucursal
);

public sealed record UpdateProductRequest(
    string? Name,
    string? Description,
    Guid? CategoryId,
    Guid? BrandId,
    Guid? SupplierId,
    Guid? TaxCategoryId,
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
    Guid? TaxCategoryId,
    string? TaxCategoryName,
    decimal CostPrice,
    decimal SellingPrice,
    string UnitOfMeasure,
    int Stock,
    int MinStock,
    int MaxStock,
    string? Location,
    string? Barcode,
    bool IsActive,
    Guid BranchId,
    Guid WarehouseId,
    string? WarehouseName
);

public sealed record ProductListResponse(
    Guid Id,
    string Code,
    string Name,
    string? CategoryName,
    decimal SellingPrice,
    decimal TaxRate,
    int Stock,
    int MinStock,
    bool IsActive,
    Guid WarehouseId,
    string? WarehouseName
);

public sealed record ProductFilterRequest(
    string? Search,
    Guid? CategoryId,
    Guid? BrandId,
    bool? LowStock,
    bool? IsActive,
    Guid? WarehouseId,   // null = bodega activa del contexto (o todas si no hay)
    int? Page = 1,
    int? PageSize = 20
);
