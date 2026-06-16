using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class FuelRefillService
{
    private readonly IFuelRefillRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public FuelRefillService(IFuelRefillRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<FuelRefillResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<FuelRefillResponse>>(items);
    }

    public async Task<FuelRefillResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<FuelRefillResponse>(item);
    }

    public async Task<FuelRefillResponse> CreateAsync(CreateFuelRefillRequest request)
    {
        var entity = _mapper.Map<FuelRefill>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FuelRefillResponse>(entity);
    }

    public async Task<FuelRefillResponse?> UpdateAsync(Guid id, UpdateFuelRefillRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FuelRefillResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return false;
        await _repo.DeleteAsync(entity);
        await _repo.SaveChangesAsync();
        return true;
    }
}
