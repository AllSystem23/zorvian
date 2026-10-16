namespace Zorvian.Core.Entities;

/// <summary>
/// Transferencia de stock entre bodegas (sucursales) de la misma compañía.
/// No tiene columna BranchId: por naturaleza abarca DOS sucursales, el filtro de
/// sucursal en listados es "es origen O destino" (explícito en el repositorio) y
/// RLS solo aplica el alcance de tenant.
/// Flujo: draft → in_transit (despacho, baja en origen) → completed (recepción,
/// alta en destino) o cancelled (solo desde draft).
/// </summary>
public sealed class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty; // TR-YYYYMMDD-NNNN
    public Guid FromBranchId { get; set; }
    public Branch FromBranch { get; set; } = null!;
    public Guid ToBranchId { get; set; }
    public Branch ToBranch { get; set; } = null!;
    public string Status { get; set; } = "draft"; // draft | in_transit | completed | cancelled
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<StockTransferDetail> Details { get; set; } = [];
}
