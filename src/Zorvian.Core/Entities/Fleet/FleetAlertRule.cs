namespace Zorvian.Core.Entities.Fleet;

/// <summary>
/// Configurable automation rules for fleet alerts.
/// Each rule defines a condition, category, severity, and action.
/// </summary>
public sealed class FleetAlertRule : BaseEntity
{
    /// <summary>Rule name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Category: Document, License, Maintenance, Fuel, Delivery, Safety, Budget</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>What triggers this rule: DocumentExpiry, LicenseExpiry, MaintenanceOverdue, FuelAnomaly, HighCost, etc.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Threshold value (days before expiry, % above average, etc.)</summary>
    public decimal ThresholdValue { get; set; }

    /// <summary>Severity when triggered</summary>
    public string Severity { get; set; } = "warning";

    /// <summary>Whether push notification should be sent</summary>
    public bool PushNotification { get; set; } = true;

    /// <summary>Whether in-app notification should be sent</summary>
    public bool InAppNotification { get; set; } = true;

    /// <summary>Whether the rule is currently active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Roles that receive the notification (comma-separated)</summary>
    public string? NotifyRoles { get; set; }
}
