using AutoMapper;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class SupplierService
{
    private readonly ISupplierRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public SupplierService(ISupplierRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<SupplierResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var suppliers = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<SupplierResponse>>(suppliers);
    }

    public async Task<PagedResult<SupplierResponse>> GetFilteredAsync(string? search, int page = 1, int pageSize = 20)
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var items = await _repo.GetFilteredAsync(search, companyId, page, pageSize);
        var total = await _repo.GetFilteredCountAsync(search, companyId);

        return new PagedResult<SupplierResponse>(
            _mapper.Map<List<SupplierResponse>>(items),
            total, page, pageSize
        );
    }

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        var companyId = Guid.Parse(_tenant.TenantId);

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            var existing = await _repo.GetByTaxIdAsync(request.TaxId, companyId);
            if (existing is not null)
                throw new InvalidOperationException($"Ya existe un proveedor con el RUC/NIT {request.TaxId}");
        }

        var supplier = _mapper.Map<Supplier>(request);
        supplier.CompanyId = companyId;
        supplier.Code = await _repo.GenerateCodeAsync(companyId);

        await _repo.AddAsync(supplier);
        await _repo.SaveChangesAsync();

        return _mapper.Map<SupplierResponse>(supplier);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request)
    {
        var supplier = await _repo.GetByIdAsync(id);
        if (supplier is null) return null;

        var companyId = Guid.Parse(_tenant.TenantId);

        if (!string.IsNullOrWhiteSpace(request.TaxId) && request.TaxId != supplier.TaxId)
        {
            var existing = await _repo.GetByTaxIdAsync(request.TaxId, companyId);
            if (existing is not null && existing.Id != id)
                throw new InvalidOperationException($"Ya existe un proveedor con el RUC/NIT {request.TaxId}");
        }

        _mapper.Map(request, supplier);
        await _repo.SaveChangesAsync();

        return _mapper.Map<SupplierResponse>(supplier);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var supplier = await _repo.GetByIdAsync(id);
        if (supplier is null) return false;

        await _repo.DeleteAsync(supplier);
        await _repo.SaveChangesAsync();
        return true;
    }
}
