using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface IWarehouseRepository
{
    Task<Warehouse?> GetByIdAsync(Guid id);

    /// <summary>Bodega por defecto (IsDefault) de la sucursal; fallback a la primera activa.</summary>
    Task<Warehouse?> GetDefaultAsync(Guid branchId);

    Task<List<Warehouse>> GetByBranchAsync(Guid branchId);

    /// <summary>
    /// <paramref name="branchId"/> null = todas las sucursales de la compañía.
    /// Orden: por defecto primero, luego nombre.
    /// </summary>
    Task<(List<Warehouse> Items, int Total)> GetFilteredAsync(
        string? search, Guid? branchId, bool? isActive, int page, int pageSize);

    /// <summary>Existe otra bodega con ese nombre en la sucursal (case-insensitive).</summary>
    Task<bool> ExistsNameAsync(Guid branchId, string name, Guid? excludeId = null);

    /// <summary>Total de bodegas (activas o no) de la sucursal — guardas de borrado.</summary>
    Task<int> CountByBranchAsync(Guid branchId);

    /// <summary>Cuenta productos reales con stock en la bodega — guarda de borrado.</summary>
    Task<int> CountProductsAsync(Guid warehouseId);

    Task AddAsync(Warehouse warehouse);
    Task UpdateAsync(Warehouse warehouse);
    Task DeleteAsync(Warehouse warehouse);
    Task SaveChangesAsync();
}
