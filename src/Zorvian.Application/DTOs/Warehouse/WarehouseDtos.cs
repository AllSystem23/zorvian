namespace Zorvian.Application.DTOs.Warehouse;

public sealed record CreateWarehouseRequest(
    Guid? BranchId,       // null = sucursal activa del contexto
    string Name,
    string? Code,
    string? Address,
    bool? IsDefault       // true = promover a bodega por defecto de la sucursal
);

public sealed record UpdateWarehouseRequest(
    string? Name,
    string? Code,
    string? Address,
    bool? IsActive
);

public sealed record WarehouseResponse(
    Guid Id,
    Guid BranchId,
    string? BranchName,
    string Name,
    string? Code,
    string? Address,
    bool IsActive,
    bool IsDefault
);

public sealed record WarehouseFilterRequest(
    Guid? BranchId,
    string? Search,
    bool? IsActive,
    int? Page = 1,
    int? PageSize = 50
);
