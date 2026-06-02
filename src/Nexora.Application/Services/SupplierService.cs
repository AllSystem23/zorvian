using AutoMapper;
using Nexora.Application.DTOs.Inventory;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

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

    public async Task<SupplierResponse> CreateAsync(CreateSupplierRequest request)
    {
        var supplier = _mapper.Map<Supplier>(request);
        supplier.CompanyId = Guid.Parse(_tenant.TenantId);

        var count = (await _repo.GetAllAsync(Guid.Parse(_tenant.TenantId))).Count;
        supplier.Code = $"PROV-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D3}";

        await _repo.AddAsync(supplier);
        await _repo.SaveChangesAsync();

        return _mapper.Map<SupplierResponse>(supplier);
    }

    public async Task<SupplierResponse?> UpdateAsync(Guid id, UpdateSupplierRequest request)
    {
        var supplier = await _repo.GetByIdAsync(id);
        if (supplier is null) return null;

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
