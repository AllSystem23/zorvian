namespace Zorvian.Application.DTOs.Warranty;

public sealed record WarrantyDashboardResponse(
    int TotalActive,
    int TotalBreachedSla,
    int RegisteredCount,
    int InDiagnosisCount,
    int InRepairCount,
    int ReadyForDeliveryCount
);

/// <summary>Raw SQL result: all warranty dashboard counts in a single round-trip.</summary>
public sealed class WarrantyDashboardScalars
{
    public int TotalActive { get; set; }
    public int TotalBreachedSla { get; set; }
    public int RegisteredCount { get; set; }
    public int InDiagnosisCount { get; set; }
    public int InRepairCount { get; set; }
    public int ReadyForDeliveryCount { get; set; }
}
