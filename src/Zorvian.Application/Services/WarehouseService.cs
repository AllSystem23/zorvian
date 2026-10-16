using Zorvian.Application.DTOs.Warehouse;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

/// <summary>
/// Bodegas: ubicaciones físicas de stock dentro de una sucursal
/// (Compañía → Sucursales → Bodegas). Garantía clave: toda sucursal que
/// reciba productos/transferencias termina con UNA bodega IsDefault
/// ("Bodega Principal") vía <see cref="EnsureDefaultAsync"/>.
/// </summary>
public sealed class WarehouseService
{
    private readonly IWarehouseRepository _repo;
    private readonly IBranchValidator _branchValidator;
    private readonly ITenantContext _tenant;

    public WarehouseService(
        IWarehouseRepository repo,
        IBranchValidator branchValidator,
        ITenantContext tenant)
    {
        _repo = repo;
        _branchValidator = branchValidator;
        _tenant = tenant;
    }

    public async Task<List<WarehouseResponse>> GetAllAsync(Guid? branchId = null)
    {
        var effectiveBranchId = branchId ?? _tenant.ResolveBranchId();
        var (items, _) = await _repo.GetFilteredAsync(null, effectiveBranchId, null, 1, 1000);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<WarehouseResponse?> GetByIdAsync(Guid id)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        return warehouse is null ? null : MapToResponse(warehouse);
    }

    public async Task<List<WarehouseResponse>> GetFilteredAsync(WarehouseFilterRequest filter)
    {
        var page = filter.Page ?? 1;
        var pageSize = filter.PageSize ?? 50;
        // El filtro explícito manda; sin él se usa la sucursal activa del contexto.
        var effectiveBranchId = filter.BranchId ?? _tenant.ResolveBranchId();
        var (items, _) = await _repo.GetFilteredAsync(filter.Search, effectiveBranchId, filter.IsActive, page, pageSize);
        return items.Select(MapToResponse).ToList();
    }

    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("El nombre de la bodega es obligatorio.");

        var name = request.Name.Trim();
        var branchId = await _branchValidator.ResolveForWriteAsync(request.BranchId)
            ?? throw new InvalidOperationException("Debe indicar la sucursal de la bodega.");

        if (await _repo.ExistsNameAsync(branchId, name))
            throw new InvalidOperationException("Ya existe una bodega con ese nombre en esta sucursal.");

        var existingCount = await _repo.CountByBranchAsync(branchId);
        var isDefault = existingCount == 0 || request.IsDefault == true;

        var tenantId = _tenant.TenantId.ToString() ?? string.Empty;
        var warehouse = new Warehouse
        {
            BranchId = branchId,
            Name = name,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            IsActive = true,
            IsDefault = isDefault,
            TenantId = tenantId,
        };
        if (Guid.TryParse(tenantId, out var companyId))
            warehouse.CompanyId = companyId;

        if (isDefault)
            await DemoteOtherDefaultsAsync(branchId, excludeId: null);

        await _repo.AddAsync(warehouse);
        await _repo.SaveChangesAsync();

        return MapToResponse(warehouse);
    }

    public async Task<WarehouseResponse?> UpdateAsync(Guid id, UpdateWarehouseRequest request)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        if (warehouse is null) return null;

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim();
            if (await _repo.ExistsNameAsync(warehouse.BranchId, name, excludeId: id))
                throw new InvalidOperationException("Ya existe una bodega con ese nombre en esta sucursal.");
            warehouse.Name = name;
        }

        if (request.Code is not null) warehouse.Code = request.Code.Trim();
        if (request.Address is not null) warehouse.Address = request.Address.Trim();
        if (request.IsActive is not null) warehouse.IsActive = request.IsActive.Value;

        await _repo.SaveChangesAsync();
        return MapToResponse(warehouse);
    }

    /// <summary>
    /// Borra una bodega vacía. Guardas: no la default con otras bodegas
    /// (reasigne primero) y no si tiene productos asociados (la sucursal
    /// tiene bodegas derivadas por migración; sugerir desactivar).
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var warehouse = await _repo.GetByIdAsync(id);
        if (warehouse is null) return false;

        var productCount = await _repo.CountProductsAsync(id);
        if (productCount > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar la bodega '{warehouse.Name}': tiene {productCount} producto(s) asociado(s). Desactívela en su lugar.");

        var branchCount = await _repo.CountByBranchAsync(warehouse.BranchId);
        if (warehouse.IsDefault && branchCount > 1)
            throw new InvalidOperationException(
                "No se puede eliminar la bodega por defecto: primero defina otra bodega como defecto.");

        await _repo.DeleteAsync(warehouse);
        await _repo.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Da de baja (soft-delete) todas las bodegas de la sucursal. Se invoca
    /// desde <c>BranchService.DeleteAsync</c> para no dejar bodegas huérfanas
    /// apuntando a una sucursal eliminada.
    /// </summary>
    public async Task SoftDeleteByBranchAsync(Guid branchId)
    {
        var warehouses = await _repo.GetByBranchAsync(branchId);
        foreach (var warehouse in warehouses)
        {
            await _repo.DeleteAsync(warehouse);
        }
        if (warehouses.Count > 0)
        {
            await _repo.SaveChangesAsync();
        }
    }

    /// <summary>Promueve la bodega a "por defecto" de su sucursal (demueve a las demás).</summary>
    public async Task<WarehouseResponse> SetDefaultAsync(Guid id)
    {
        var warehouse = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Bodega no encontrada");

        if (!warehouse.IsActive)
            throw new InvalidOperationException("No se puede definir como defecto una bodega inactiva.");

        await DemoteOtherDefaultsAsync(warehouse.BranchId, excludeId: id);
        warehouse.IsDefault = true;
        await _repo.SaveChangesAsync();

        return MapToResponse(warehouse);
    }

    /// <summary>
    /// Garantiza que la sucursal tenga una bodega por defecto. Idempotente:
    /// si ya existe default la devuelve; si hay bodegas sin default promueve la
    /// primera; si no hay ninguna crea "Bodega Principal".
    /// </summary>
    public async Task<Warehouse> EnsureDefaultAsync(Guid branchId)
    {
        var existingDefault = await _repo.GetDefaultAsync(branchId);
        if (existingDefault is not null)
            return existingDefault;

        var all = await _repo.GetByBranchAsync(branchId);
        var first = all.FirstOrDefault(w => w.IsActive) ?? all.FirstOrDefault();
        if (first is not null)
        {
            await DemoteOtherDefaultsAsync(branchId, excludeId: first.Id);
            first.IsDefault = true;
            await _repo.SaveChangesAsync();
            return first;
        }

        var tenantId = _tenant.TenantId.ToString() ?? string.Empty;
        var warehouse = new Warehouse
        {
            BranchId = branchId,
            Name = "Bodega Principal",
            IsActive = true,
            IsDefault = true,
            TenantId = tenantId,
        };
        if (Guid.TryParse(tenantId, out var companyId))
            warehouse.CompanyId = companyId;

        await _repo.AddAsync(warehouse);
        await _repo.SaveChangesAsync();
        return warehouse;
    }

    /// <summary>
    /// Resuelve la bodega de ESCRITURA para una sucursal dada:
    /// la pedida (validada contra la sucursal) o la default (creándola si falta).
    /// </summary>
    public async Task<Warehouse> ResolveForWriteAsync(Guid? requestedWarehouseId, Guid branchId)
    {
        if (requestedWarehouseId is { } requested && requested != Guid.Empty)
        {
            var warehouse = await _repo.GetByIdAsync(requested)
                ?? throw new InvalidOperationException("La bodega indicada no existe.");

            if (warehouse.BranchId != branchId)
                throw new InvalidOperationException("La bodega indicada no pertenece a la sucursal seleccionada.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("La bodega indicada está inactiva.");

            return warehouse;
        }

        return await EnsureDefaultAsync(branchId);
    }

    private async Task DemoteOtherDefaultsAsync(Guid branchId, Guid? excludeId)
    {
        var (items, _) = await _repo.GetFilteredAsync(null, branchId, null, 1, 1000);
        foreach (var other in (items ?? new List<Warehouse>()).Where(w => w.IsDefault && w.Id != excludeId))
        {
            other.IsDefault = false;
            await _repo.UpdateAsync(other);
        }
    }

    private static WarehouseResponse MapToResponse(Warehouse w) => new(
        w.Id, w.BranchId, w.Branch?.Name,
        w.Name, w.Code, w.Address,
        w.IsActive, w.IsDefault);
}
