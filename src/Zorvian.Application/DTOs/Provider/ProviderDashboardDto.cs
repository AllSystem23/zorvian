namespace Zorvian.Application.DTOs.Provider;

public sealed record ProviderDashboardDto(
    int TotalProviders,
    int ActiveProviders,
    int TotalContracts,
    int ActiveContracts,
    decimal TotalContractValue,
    int PendingMilestones,
    int OverdueMilestones,
    int CompletedMilestones,
    int PendingInvoices,
    decimal PendingInvoiceAmount,
    List<ProviderContractSummary> RecentContracts,
    List<ProviderMilestoneByStatus> MilestonesByStatus,
    List<ProviderTopProvider> TopProviders
);

public sealed record ProviderContractSummary(
    Guid Id,
    string ContractNumber,
    string ContractName,
    string ProviderName,
    decimal TotalAmount,
    string Status,
    DateOnly StartDate,
    DateOnly? EndDate
);

public sealed record ProviderMilestoneByStatus(
    string Status,
    int Count
);

public sealed record ProviderTopProvider(
    Guid Id,
    string BusinessName,
    int ContractCount,
    decimal TotalValue
);

public sealed record ProviderNotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    string Severity,
    Guid ReferenceId,
    string EntityType
);
