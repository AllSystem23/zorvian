using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class FuelTypeService
{
    private readonly IFuelTypeRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public FuelTypeService(IFuelTypeRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<FuelTypeResponse>> GetAllAsync()
    {
        var types = await _repo.GetAllAsync();
        return _mapper.Map<List<FuelTypeResponse>>(types);
    }

    public async Task<FuelTypeResponse?> GetByIdAsync(Guid id)
    {
        var type = await _repo.GetByIdAsync(id);
        return type is null ? null : _mapper.Map<FuelTypeResponse>(type);
    }

    public async Task<FuelTypeResponse> CreateAsync(CreateFuelTypeRequest request)
    {
        var type = _mapper.Map<FuelType>(request);
        var tenantId = _tenant.TenantId.ToString() ?? string.Empty;
        if (Guid.TryParse(tenantId, out var companyId))
            type.CompanyId = companyId;
        type.TenantId = tenantId;
        await _repo.AddAsync(type);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FuelTypeResponse>(type);
    }

    public async Task<FuelTypeResponse?> UpdateAsync(Guid id, UpdateFuelTypeRequest request)
    {
        var type = await _repo.GetByIdAsync(id);
        if (type is null) return null;
        _mapper.Map(request, type);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FuelTypeResponse>(type);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var type = await _repo.GetByIdAsync(id);
        if (type is null) return false;
        await _repo.DeleteAsync(type);
        await _repo.SaveChangesAsync();
        return true;
    }
}
