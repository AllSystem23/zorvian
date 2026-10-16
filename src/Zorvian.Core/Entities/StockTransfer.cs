namespace Zorvian.Core.Entities;

/// <summary>
/// Transferencia de stock entre DOS bodegas (misma sucursal o de sucursales
/// distintas de la misma compañía). No tiene columna BranchId: abarca DOS
/// sucursales, el filtro en listados es "es origen O destino" (explícito en
/// el repositorio) y RLS solo aplica el alcance de tenant.
/// Flujo: draft → in_transit (despacho, baja en origen) →
/// partially_received (recepción parcial, re-receivable) → completed (todo
/// recibido); cancelled solo desde draft.
/// FromBranchId/ToBranchId se derivan de las bodegas al crear (se persisten
/// para el filtro de sucursal y RLS).
/// </summary>
public sealed class StockTransfer : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty; // TR-YYYYMMDD-NNNN

    public Guid FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = null!;
    public Guid ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = null!;

    public Guid FromBranchId { get; set; }
    public Branch FromBranch { get; set; } = null!;
    public Guid ToBranchId { get; set; }
    public Branch ToBranch { get; set; } = null!;
    public string Status { get; set; } = "draft"; // draft | in_transit | partially_received | completed | cancelled
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<StockTransferDetail> Details { get; set; } = [];
}
