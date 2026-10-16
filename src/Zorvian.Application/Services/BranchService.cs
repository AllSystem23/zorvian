using AutoMapper;
using Zorvian.Application.DTOs.Branch;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BranchService
{
    private readonly IBranchRepository _repo;
    private readonly WarehouseService _warehouseService;
    private readonly IProductRepository _productRepo;
    private readonly ISaleRepository _saleRepo;
    private readonly IPurchaseRepository _purchaseRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public BranchService(
        IBranchRepository repo,
        WarehouseService warehouseService,
        IProductRepository productRepo,
        ISaleRepository saleRepo,
        IPurchaseRepository purchaseRepo,
        IEmployeeRepository employeeRepo,
        ITenantContext tenant,
        IMapper mapper)
    {
        _repo = repo;
        _warehouseService = warehouseService;
        _productRepo = productRepo;
        _saleRepo = saleRepo;
        _purchaseRepo = purchaseRepo;
        _employeeRepo = employeeRepo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<BranchResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var branches = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<BranchResponse>>(branches);
    }

    public async Task<BranchResponse?> GetByIdAsync(Guid id)
    {
        var branch = await _repo.GetByIdAsync(id);
        return branch is null ? null : _mapper.Map<BranchResponse>(branch);
    }

    public async Task<BranchResponse> CreateAsync(CreateBranchRequest request)
    {
        var branch = _mapper.Map<Branch>(request);
        var tenantId = _tenant.TenantId.ToString() ?? string.Empty;
        if (Guid.TryParse(tenantId, out var companyId))
            branch.CompanyId = companyId;
        branch.TenantId = tenantId;

        await _repo.AddAsync(branch);
        await _repo.SaveChangesAsync();

        // Toda sucursal nueva nace con su bodega por defecto ("Bodega Principal"):
        // es donde caen los productos creados sin bodega explícita.
        await _warehouseService.EnsureDefaultAsync(branch.Id);

        return _mapper.Map<BranchResponse>(branch);
    }

    public async Task<BranchResponse?> UpdateAsync(Guid id, UpdateBranchRequest request)
    {
        var branch = await _repo.GetByIdAsync(id);
        if (branch is null) return null;

        _mapper.Map(request, branch);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BranchResponse>(branch);
    }

    /// <summary>
    /// Soft-delete con guardas: no se puede eliminar una sucursal que tenga
    /// productos ACTIVOS asignados (hay que moverlos o desactivarlos),
    /// historial de ventas o compras (rastro fiscal/contable — desactívela en
    /// su lugar) ni empleados asignados (reubíquelos primero). Al eliminar,
    /// sus bodegas se dan de baja en cascada para no dejar bodegas huérfanas
    /// apuntando a una sucursal muerta.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        var branch = await _repo.GetByIdAsync(id);
        if (branch is null) return false;

        var activeProducts = await _productRepo.GetFilteredCountAsync(
            search: null, categoryId: null, brandId: null, lowStock: null,
            isActive: true, branchId: id, warehouseId: null);
        if (activeProducts > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar la sucursal '{branch.Name}': tiene {activeProducts} producto(s) activo(s). "
                + "Muévalos a otra sucursal o desactívelos primero.");

        var sales = await _saleRepo.GetFilteredCountAsync(
            clientId: null, saleType: null, status: null,
            fromDate: null, toDate: null, search: null, branchId: id);
        if (sales > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar la sucursal '{branch.Name}': tiene {sales} venta(s) registradas "
                + "(historial fiscal). Desactívela en su lugar.");

        var purchases = await _purchaseRepo.GetFilteredCountAsync(
            supplierId: null, status: null, fromDate: null, toDate: null, search: null, branchId: id);
        if (purchases > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar la sucursal '{branch.Name}': tiene {purchases} compra(s) registradas "
                + "(historial fiscal). Desactívela en su lugar.");

        var employees = await _employeeRepo.GetFilteredCountAsync(
            search: null, status: null, departmentId: null, branchId: id);
        if (employees > 0)
            throw new InvalidOperationException(
                $"No se puede eliminar la sucursal '{branch.Name}': tiene {employees} empleado(s) asignados. "
                + "Reubíquelos a otra sucursal primero.");

        await _warehouseService.SoftDeleteByBranchAsync(id);
        await _repo.DeleteAsync(branch);
        await _repo.SaveChangesAsync();
        return true;
    }
}
