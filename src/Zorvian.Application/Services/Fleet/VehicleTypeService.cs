using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class VehicleTypeService
{
    private readonly IVehicleTypeRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public VehicleTypeService(IVehicleTypeRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<VehicleTypeResponse>> GetAllAsync()
    {
        var types = await _repo.GetAllAsync();
        return _mapper.Map<List<VehicleTypeResponse>>(types);
    }

    public async Task<VehicleTypeResponse?> GetByIdAsync(Guid id)
    {
        var type = await _repo.GetByIdAsync(id);
        return type is null ? null : _mapper.Map<VehicleTypeResponse>(type);
    }

    public async Task<VehicleTypeResponse> CreateAsync(CreateVehicleTypeRequest request)
    {
        var type = _mapper.Map<VehicleType>(request);
        var tenantId = _tenant.TenantId.ToString() ?? string.Empty;
        if (Guid.TryParse(tenantId, out var companyId))
            type.CompanyId = companyId;
        type.TenantId = tenantId;
        await _repo.AddAsync(type);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VehicleTypeResponse>(type);
    }

    public async Task<VehicleTypeResponse?> UpdateAsync(Guid id, UpdateVehicleTypeRequest request)
    {
        var type = await _repo.GetByIdAsync(id);
        if (type is null) return null;
        _mapper.Map(request, type);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VehicleTypeResponse>(type);
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
