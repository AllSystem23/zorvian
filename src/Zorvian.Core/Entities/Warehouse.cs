namespace Zorvian.Core.Entities;

/// <summary>
/// Bodega: ubicación física de stock DENTRO de una sucursal.
/// Compañía → Sucursales → Bodegas (una sucursal puede tener varias).
/// El stock de un producto vive por bodega (Product.WarehouseId, único por
/// (Code, WarehouseId)); BranchId en Product se mantiene denormalizado
/// (= Warehouse.BranchId) para el alcance de sucursal y RLS existentes.
/// Exactamente UNA bodega por sucursal es IsDefault (la "Principal"):
/// es donde caen los productos creados sin bodega explícita.
/// </summary>
public sealed class Warehouse : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
