namespace Zorvian.Core.Entities;

/// <summary>
/// Solicitud de Cotización: un área pide a Compras cotizar/ comprar productos.
/// Flujo: draft → submitted → approved/rejected (o cancelled desde draft/submitted).
/// </summary>
public sealed class PurchaseRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty; // SC-YYYYMMDD-NNNN
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = "normal"; // low | normal | high | urgent
    public DateOnly? NeededByDate { get; set; }
    public string Status { get; set; } = "draft"; // draft | submitted | approved | rejected | cancelled
    public string? RejectionReason { get; set; }
    public decimal EstimatedTotal { get; set; }
    public string? Notes { get; set; }
    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public ICollection<PurchaseRequestDetail> Details { get; set; } = [];
}
