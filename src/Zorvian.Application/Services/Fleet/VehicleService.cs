using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class VehicleService
{
    private readonly IVehicleRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public VehicleService(IVehicleRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<VehicleResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var vehicles = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<VehicleResponse>>(vehicles);
    }

    public async Task<VehicleResponse?> GetByIdAsync(Guid id)
    {
        var vehicle = await _repo.GetByIdAsync(id);
        return vehicle is null ? null : _mapper.Map<VehicleResponse>(vehicle);
    }

    public async Task<VehicleResponse> CreateAsync(CreateVehicleRequest request)
    {
        var vehicle = _mapper.Map<Vehicle>(request);
        vehicle.Status = "Active";
        await _repo.AddAsync(vehicle);
        await _repo.SaveChangesAsync();
        var created = await _repo.GetByIdAsync(vehicle.Id);
        return _mapper.Map<VehicleResponse>(created!);
    }

    public async Task<VehicleResponse?> UpdateAsync(Guid id, UpdateVehicleRequest request)
    {
        var vehicle = await _repo.GetByIdAsync(id);
        if (vehicle is null) return null;
        _mapper.Map(request, vehicle);
        await _repo.SaveChangesAsync();
        return _mapper.Map<VehicleResponse>(vehicle);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var vehicle = await _repo.GetByIdAsync(id);
        if (vehicle is null) return false;
        await _repo.DeleteAsync(vehicle);
        await _repo.SaveChangesAsync();
        return true;
    }
}
