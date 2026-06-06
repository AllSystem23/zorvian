namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyDashboardResponse(
    int TotalActive,
    int TotalBreachedSla,
    int RegisteredCount,
    int InDiagnosisCount,
    int InRepairCount,
    int ReadyForDeliveryCount
);
