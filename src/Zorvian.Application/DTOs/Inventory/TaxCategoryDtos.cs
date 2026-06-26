namespace Zorvian.Application.DTOs.Inventory;

public sealed record CreateTaxCategoryRequest(
    string Name,
    decimal Rate,
    string SalesAccountCode,
    string VatAccountCode
);

public sealed record UpdateTaxCategoryRequest(
    string? Name,
    decimal? Rate,
    string? SalesAccountCode,
    string? VatAccountCode
);

public sealed record TaxCategoryResponse(
    Guid Id,
    string Name,
    decimal Rate,
    string SalesAccountCode,
    string VatAccountCode
);
