using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class MaintenanceScheduleService
{
    private readonly IMaintenanceScheduleRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public MaintenanceScheduleService(IMaintenanceScheduleRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<MaintenanceScheduleResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var items = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<MaintenanceScheduleResponse>>(items);
    }

    public async Task<MaintenanceScheduleResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<MaintenanceScheduleResponse>(item);
    }

    public async Task<MaintenanceScheduleResponse> CreateAsync(CreateMaintenanceScheduleRequest request)
    {
        var entity = _mapper.Map<MaintenanceSchedule>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        var created = await _repo.GetByIdAsync(entity.Id);
        return _mapper.Map<MaintenanceScheduleResponse>(created!);
    }

    public async Task<MaintenanceScheduleResponse?> UpdateAsync(Guid id, UpdateMaintenanceScheduleRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<MaintenanceScheduleResponse>(entity);
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
