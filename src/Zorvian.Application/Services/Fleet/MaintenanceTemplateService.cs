using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class MaintenanceTemplateService
{
    private readonly IMaintenanceTemplateRepository _repo;
    private readonly IMapper _mapper;

    public MaintenanceTemplateService(IMaintenanceTemplateRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<MaintenanceTemplateResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return _mapper.Map<List<MaintenanceTemplateResponse>>(items);
    }

    public async Task<MaintenanceTemplateResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<MaintenanceTemplateResponse>(item);
    }

    public async Task<MaintenanceTemplateResponse> CreateAsync(CreateMaintenanceTemplateRequest request)
    {
        var entity = _mapper.Map<MaintenanceTemplate>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<MaintenanceTemplateResponse>(entity);
    }

    public async Task<MaintenanceTemplateResponse?> UpdateAsync(Guid id, UpdateMaintenanceTemplateRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<MaintenanceTemplateResponse>(entity);
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
