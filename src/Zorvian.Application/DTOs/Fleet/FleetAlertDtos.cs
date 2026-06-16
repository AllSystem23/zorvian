namespace Zorvian.Application.DTOs.Fleet;

// ── FleetAlert DTOs ──

public sealed record FleetAlertResponse(
    Guid Id,
    string Category,
    string Severity,
    string? EntityType,
    Guid? EntityId,
    string Title,
    string Message,
    string Status,
    bool NotificationSent,
    string? AcknowledgedBy,
    DateTime? AcknowledgedAt,
    string? AcknowledgementNotes,
    DateTime CreatedAt);

public sealed record AcknowledgeAlertRequest(
    string? Notes);

public sealed record CreateFleetAlertRequest(
    string Category,
    string Severity,
    string? EntityType,
    Guid? EntityId,
    string Title,
    string Message);

// ── FleetAlertRule DTOs ──

public sealed record FleetAlertRuleResponse(
    Guid Id,
    string Name,
    string Category,
    string EventType,
    decimal ThresholdValue,
    string Severity,
    bool PushNotification,
    bool InAppNotification,
    bool IsActive,
    string? NotifyRoles);

public sealed record CreateFleetAlertRuleRequest(
    string Name,
    string Category,
    string EventType,
    decimal ThresholdValue,
    string Severity = "warning",
    bool PushNotification = true,
    bool InAppNotification = true,
    string? NotifyRoles = null);

public sealed record UpdateFleetAlertRuleRequest(
    string? Name = null,
    string? Category = null,
    string? EventType = null,
    decimal? ThresholdValue = null,
    string? Severity = null,
    bool? PushNotification = null,
    bool? InAppNotification = null,
    bool? IsActive = null,
    string? NotifyRoles = null);

// ── Fleet Alert Summary ──

public sealed record FleetAlertSummary(
    int ActiveAlerts,
    int CriticalAlerts,
    int WarningAlerts,
    int InfoAlerts,
    int UnacknowledgedAlerts,
    List<FleetAlertResponse> RecentAlerts);

// ── Driver Blocking DTOs ──

public sealed record BlockDriverRequest(
    string Reason,
    string? BlockedUntil = null);

public sealed record DriverBlockResponse(
    Guid DriverId,
    string DriverName,
    bool IsBlocked,
    string? BlockReason,
    DateTime? BlockedAt,
    string? BlockedBy,
    DateTime? BlockedUntil);
