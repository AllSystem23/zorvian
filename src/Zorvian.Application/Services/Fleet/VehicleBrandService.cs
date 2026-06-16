using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class VehicleBrandService
{
    private readonly IVehicleBrandRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public VehicleBrandService(IVehicleBrandRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<VehicleBrandResponse>> GetAllAsync()
    {
        var brands = await _repo.GetAllAsync();
        return _mapper.Map<List<VehicleBrandResponse>>(brands);
    }

    public async Task<VehicleBrandResponse?> GetByIdAsync(Guid id)
    {
        var brand = await _repo.GetByIdAsync(id);
        return brand is null ? null : _mapper.Map<VehicleBrandResponse>(brand);
    }

    public async Task<VehicleBrandResponse> CreateAsync(CreateVehicleBrandRequest request)
    {
        var brand = _mapper.Map<VehicleBrand>(request);
        await _repo.AddAsync(brand);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VehicleBrandResponse>(brand);
    }

    public async Task<VehicleBrandResponse?> UpdateAsync(Guid id, UpdateVehicleBrandRequest request)
    {
        var brand = await _repo.GetByIdAsync(id);
        if (brand is null) return null;
        _mapper.Map(request, brand);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VehicleBrandResponse>(brand);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var brand = await _repo.GetByIdAsync(id);
        if (brand is null) return false;
        await _repo.DeleteAsync(brand);
        await _repo.SaveChangesAsync();
        return true;
    }
}
