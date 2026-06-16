using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class FailureTypeService
{
    private readonly IFailureTypeRepository _repo;
    private readonly IMapper _mapper;

    public FailureTypeService(IFailureTypeRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<List<FailureTypeResponse>> GetAllAsync()
    {
        var items = await _repo.GetAllAsync();
        return _mapper.Map<List<FailureTypeResponse>>(items);
    }

    public async Task<FailureTypeResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<FailureTypeResponse>(item);
    }

    public async Task<FailureTypeResponse> CreateAsync(CreateFailureTypeRequest request)
    {
        var entity = _mapper.Map<FailureType>(request);
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FailureTypeResponse>(entity);
    }

    public async Task<FailureTypeResponse?> UpdateAsync(Guid id, UpdateFailureTypeRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<FailureTypeResponse>(entity);
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
