namespace Zorvian.Application.DTOs.Commercial;

public sealed record CreateClientRequest(
    string FirstName,
    string LastName,
    string? IdentificationNumber,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? References,
    decimal? CreditLimit,
    Guid BranchId
);

public sealed record UpdateClientRequest(
    string? FirstName,
    string? LastName,
    string? IdentificationNumber,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? References,
    string? Status,
    decimal? CreditLimit
);

public sealed record ClientResponse(
    Guid Id,
    string Code,
    string FirstName,
    string LastName,
    string FullName,
    string? IdentificationNumber,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? References,
    string Status,
    decimal? CreditLimit,
    Guid BranchId
);

public sealed record ClientListResponse(
    Guid Id,
    string Code,
    string FullName,
    string? IdentificationNumber,
    string? Phone,
    string Status
);

public sealed record ClientFilterRequest(
    string? Search,
    string? Status,
    int? Page = 1,
    int? PageSize = 20
);

public sealed record ClientStatementSaleItem(
    Guid Id,
    string InvoiceNumber,
    DateTime SaleDate,
    string SaleType,
    decimal Total,
    decimal PaidAmount,
    decimal Balance,
    string Status
);

public sealed record ClientStatementCreditItem(
    Guid Id,
    string CreditNumber,
    decimal FinancedAmount,
    decimal Balance,
    decimal PaidAmount,
    DateOnly StartDate,
    DateOnly? NextDueDate,
    string Status
);

public sealed record ClientStatementResponse(
    Guid ClientId,
    string ClientName,
    string ClientCode,
    string? ClientPhone,
    decimal? CreditLimit,
    int TotalSales,
    int ActiveCredits,
    decimal TotalBalance,
    decimal OverdueBalance,
    List<ClientStatementSaleItem> RecentSales,
    List<ClientStatementCreditItem> ActiveCreditsList
);
