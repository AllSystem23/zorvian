using Zorvian.Core.Entities;

namespace Zorvian.Application.DTOs.FixedAssets;

public sealed record FixedAssetResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? CategoryId,
    string? CategoryName,
    string? SerialNumber,
    string? Barcode,
    string? Brand,
    string? Model,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    Guid? SupplierId,
    string? SupplierName,
    string? InvoiceReference,
    Guid? PurchaseId,
    int UsefulLifeYears,
    decimal ResidualValue,
    string DepreciationMethod,
    decimal? TotalUnits,
    decimal? UnitsProduced,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    Guid? LocationId,
    string? LocationName,
    Guid? DepartmentId,
    string? DepartmentName,
    string? AssignedTo,
    string Status,
    string? ImageUrl,
    DateTime CreatedAt,
    List<DepreciationEntryResponse> DepreciationEntries,
    List<AssetRevaluationResponse> Revaluations,
    List<AssetMaintenanceResponse> Maintenances,
    AssetDisposalResponse? Disposal
);

public sealed record FixedAssetListResponse(
    Guid Id,
    string Code,
    string Name,
    string? CategoryName,
    string? SerialNumber,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    string Status,
    string? LocationName,
    string? AssignedTo
);

public sealed record DepreciationEntryResponse(
    Guid Id,
    DateTime PeriodDate,
    decimal Amount,
    decimal AccumulatedDepreciation,
    decimal NetBookValue,
    Guid? AccountingEntryId
);

public sealed record AssetRevaluationResponse(
    Guid Id,
    DateTime RevaluationDate,
    decimal PreviousValue,
    decimal NewValue,
    decimal PreviousAccumulatedDepreciation,
    string? Reason,
    string? ApprovedBy,
    Guid? AccountingEntryId
);

public sealed record AssetMaintenanceResponse(
    Guid Id,
    DateTime MaintenanceDate,
    string MaintenanceType,
    string Description,
    decimal Cost,
    string? Provider,
    DateTime? NextMaintenanceDate,
    string Status
);

public sealed record AssetDisposalResponse(
    Guid Id,
    DateTime DisposalDate,
    string DisposalType,
    decimal? SaleAmount,
    decimal NetBookValueAtDisposal,
    decimal GainOrLoss,
    string? Reason,
    string? ApprovedBy,
    Guid? AccountingEntryId
);

public sealed record CreateFixedAssetRequest(
    string Name,
    string? Description,
    Guid? CategoryId,
    string? SerialNumber,
    string? Barcode,
    string? Brand,
    string? Model,
    DateTime AcquisitionDate,
    decimal AcquisitionCost,
    Guid? SupplierId,
    string? InvoiceReference,
    Guid? PurchaseId,
    int UsefulLifeYears,
    decimal ResidualValue,
    string DepreciationMethod,
    decimal? TotalUnits,
    Guid? LocationId,
    Guid? DepartmentId,
    string? AssignedTo,
    Guid BranchId
);

public sealed record UpdateFixedAssetRequest(
    string? Name,
    string? Description,
    Guid? CategoryId,
    string? SerialNumber,
    string? Barcode,
    string? Brand,
    string? Model,
    DateTime? AcquisitionDate,
    decimal? AcquisitionCost,
    Guid? SupplierId,
    string? InvoiceReference,
    int? UsefulLifeYears,
    decimal? ResidualValue,
    string? DepreciationMethod,
    decimal? TotalUnits,
    Guid? LocationId,
    Guid? DepartmentId,
    string? AssignedTo,
    string? Status
);

public sealed record FixedAssetFilterRequest(
    Guid? CategoryId,
    string? Status,
    Guid? LocationId,
    Guid? DepartmentId,
    string? Search,
    DateTime? FromDate,
    DateTime? ToDate,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record RunDepreciationRequest(
    DateTime PeriodDate
);

public sealed record RevalueAssetRequest(
    decimal NewValue,
    string? Reason,
    string? ApprovedBy
);

public sealed record DisposeAssetRequest(
    string DisposalType,
    decimal? SaleAmount,
    string? Reason,
    string? ApprovedBy
);

public sealed record AddMaintenanceRequest(
    DateTime MaintenanceDate,
    string MaintenanceType,
    string Description,
    decimal Cost,
    string? Provider,
    DateTime? NextMaintenanceDate,
    int? EstimatedDurationHours,
    string? Status
);

public sealed record FixedAssetSummaryResponse(
    int TotalAssets,
    int ActiveCount,
    int DisposedCount,
    decimal TotalCost,
    decimal TotalAccumulatedDepreciation,
    decimal TotalNetBookValue,
    List<CategorySummaryItem> ByCategory,
    List<StatusSummaryItem> ByStatus
);

public sealed record CategorySummaryItem(
    string CategoryName,
    int Count,
    decimal TotalCost,
    decimal TotalNetBookValue
);

public sealed record StatusSummaryItem(
    string Status,
    int Count,
    decimal TotalNetBookValue
);

public sealed record DepreciationScheduleItem(
    int Year,
    int Month,
    decimal Amount,
    decimal AccumulatedDepreciation,
    decimal NetBookValue
);

public sealed record CreateFixedAssetCategoryRequest(
    string Name,
    string? Description,
    int? DefaultUsefulLifeYears,
    string? DefaultDepreciationMethod
);

public sealed record FixedAssetCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    int? DefaultUsefulLifeYears,
    string? DefaultDepreciationMethod,
    bool IsActive
);

public sealed record CreateLocationRequest(
    string Name,
    string? Description,
    string? Address
);

public sealed record LocationResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Address,
    bool IsActive
);
