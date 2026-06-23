using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class WorkOrderService
{
    private readonly IWorkOrderRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public WorkOrderService(IWorkOrderRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<WorkOrderResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<WorkOrderResponse>>(items);
    }

    public async Task<WorkOrderResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<WorkOrderResponse>(item);
    }

    public async Task<WorkOrderResponse> CreateAsync(CreateWorkOrderRequest request)
    {
        var entity = _mapper.Map<WorkOrder>(request);
        entity.Status = "Reported";
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        var created = await _repo.GetByIdAsync(entity.Id);
        return _mapper.Map<WorkOrderResponse>(created!);
    }

    public async Task<WorkOrderResponse?> UpdateAsync(Guid id, UpdateWorkOrderRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<WorkOrderResponse>(entity);
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
