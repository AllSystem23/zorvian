using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class WorkshopService
{
    private readonly IWorkshopRepository _repo;
    private readonly IMapper _mapper;

    public WorkshopService(IWorkshopRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<WorkshopResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return _mapper.Map<List<WorkshopResponse>>(items);
    }

    public async Task<WorkshopResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<WorkshopResponse>(item);
    }

    public async Task<WorkshopResponse> CreateAsync(CreateWorkshopRequest request)
    {
        var entity = _mapper.Map<Workshop>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<WorkshopResponse>(entity);
    }

    public async Task<WorkshopResponse?> UpdateAsync(Guid id, UpdateWorkshopRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<WorkshopResponse>(entity);
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
