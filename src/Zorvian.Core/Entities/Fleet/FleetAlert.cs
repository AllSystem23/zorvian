namespace Zorvian.Core.Entities.Fleet;

public sealed class FleetAlert : BaseEntity
{
    /// <summary>Alert category: Document, License, Maintenance, Fuel, Delivery, Safety, Budget</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Severity: info, warning, critical</summary>
    public string Severity { get; set; } = "info";

    /// <summary>Related entity type (Vehicle, Driver, FleetDocument, WorkOrder, etc.)</summary>
    public string? EntityType { get; set; }

    /// <summary>Related entity ID</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Alert title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Detailed alert message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Current status: Active, Acknowledged, Dismissed, Resolved</summary>
    public string Status { get; set; } = "Active";

    /// <summary>Whether a notification has been sent for this alert</summary>
    public bool NotificationSent { get; set; }

    /// <summary>User who acknowledged/dismissed the alert</summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>When the alert was acknowledged</summary>
    public DateTime? AcknowledgedAt { get; set; }

    /// <summary>Notes from the user when acknowledging</summary>
    public string? AcknowledgementNotes { get; set; }
}
