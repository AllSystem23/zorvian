namespace Zorvian.Application.DTOs.Accounting;

public sealed record AccountResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Type,
    string NormalSide,
    Guid? ParentId,
    string? ParentName,
    int Level,
    bool IsActive,
    decimal OpeningBalance,
    decimal CurrentBalance,
    List<AccountResponse> Children
);

public sealed record CreateAccountRequest(
    string Code,
    string Name,
    string? Description,
    string Type,
    string NormalSide,
    Guid? ParentId,
    int Level,
    decimal OpeningBalance
);

public sealed record UpdateAccountRequest(
    string? Name,
    string? Description,
    bool? IsActive,
    decimal? OpeningBalance
);

public sealed record AccountLinkResponse(
    Guid Id,
    string TransactionType,
    string Role,
    Guid AccountId,
    string AccountCode,
    string AccountName
);

public sealed record CreateAccountLinkRequest(
    string TransactionType,
    string Role,
    Guid AccountId
);

public sealed record AccountingRuleResponse(
    Guid Id,
    string EventType,
    string LineType,
    string AccountRole,
    string? Formula,
    int SortOrder,
    bool IsActive
);

public sealed record CreateAccountingRuleRequest(
    string EventType,
    string LineType,
    string AccountRole,
    string? Formula,
    int SortOrder
);

public sealed record CostCenterResponse(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    bool IsActive
);

public sealed record CreateCostCenterRequest(
    string Name,
    string Code,
    string? Description
);

public sealed record UpdateCostCenterRequest(
    string? Name,
    string? Code,
    string? Description,
    bool? IsActive
);
