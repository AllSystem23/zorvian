namespace Zorvian.Core.Entities;

public sealed class StockTransferDetail : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;

    /// <summary>Producto de la sucursal ORIGEN (se resuelve/remepea al crear).</summary>
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }

    /// <summary>Costo unitario congelado al despachar (origen) para el alta en destino.</summary>
    public decimal UnitCost { get; set; }
}
