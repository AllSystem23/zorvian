using AutoMapper;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class CostCenterService
{
    private readonly ICostCenterRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CostCenterService(ICostCenterRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<CostCenterResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<CostCenterResponse>>(items);
    }

    public async Task<CostCenterResponse> CreateAsync(CreateCostCenterRequest request)
    {
        var entity = _mapper.Map<CostCenter>(request);

        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();

        return _mapper.Map<CostCenterResponse>(entity);
    }

    public async Task<CostCenterResponse?> UpdateAsync(Guid id, UpdateCostCenterRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;

        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();

        return _mapper.Map<CostCenterResponse>(entity);
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
