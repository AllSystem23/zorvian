namespace Zorvian.Core.Enums;

public enum WarrantyStatus
{
    Registered = 1,
    PendingReview = 2,
    InDiagnosis = 3,
    SentToWorkshop = 4,
    InRepair = 5,
    PendingParts = 6,
    Repaired = 7,
    ReplacementApproved = 8,
    Rejected = 9,
    ReadyForDelivery = 10,
    Delivered = 11,
    Closed = 12,
    Cancelled = 13
}

public static class WarrantyStatusExtensions
{
    public static string ToDbValue(this WarrantyStatus status) => status switch
    {
        WarrantyStatus.Registered => "registered",
        WarrantyStatus.PendingReview => "pending_review",
        WarrantyStatus.InDiagnosis => "in_diagnosis",
        WarrantyStatus.SentToWorkshop => "sent_to_workshop",
        WarrantyStatus.InRepair => "in_repair",
        WarrantyStatus.PendingParts => "pending_parts",
        WarrantyStatus.Repaired => "repaired",
        WarrantyStatus.ReplacementApproved => "replacement_approved",
        WarrantyStatus.Rejected => "rejected",
        WarrantyStatus.ReadyForDelivery => "ready_for_delivery",
        WarrantyStatus.Delivered => "delivered",
        WarrantyStatus.Closed => "closed",
        WarrantyStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static string ToSpanishName(this WarrantyStatus status) => status switch
    {
        WarrantyStatus.Registered => "Registrada",
        WarrantyStatus.PendingReview => "Pendiente de Revisión",
        WarrantyStatus.InDiagnosis => "En Diagnóstico",
        WarrantyStatus.SentToWorkshop => "Enviada a Taller",
        WarrantyStatus.InRepair => "En Reparación",
        WarrantyStatus.PendingParts => "Pendiente de Repuestos",
        WarrantyStatus.Repaired => "Reparada",
        WarrantyStatus.ReplacementApproved => "Reemplazo Aprobado",
        WarrantyStatus.Rejected => "Rechazada",
        WarrantyStatus.ReadyForDelivery => "Lista para Entrega",
        WarrantyStatus.Delivered => "Entregada",
        WarrantyStatus.Closed => "Cerrada",
        WarrantyStatus.Cancelled => "Cancelada",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
