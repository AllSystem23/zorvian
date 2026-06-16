using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetDocumentService
{
    private readonly IFleetDocumentRepository _repository;
    private readonly IMapper _mapper;

    public FleetDocumentService(IFleetDocumentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<FleetDocumentResponse>> GetAllAsync() =>
        _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetAllAsync(Guid.Empty));

    public async Task<List<FleetDocumentResponse>> GetByEntityAsync(string entityType, Guid entityId) =>
        _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetByEntityAsync(entityType, entityId));

    public async Task<FleetDocumentResponse?> GetByIdAsync(Guid id) =>
        _mapper.Map<FleetDocumentResponse?>(await _repository.GetByIdAsync(id));

    public async Task<FleetDocumentResponse> CreateAsync(CreateFleetDocumentRequest request)
    {
        var document = _mapper.Map<FleetDocument>(request);
        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();
        return _mapper.Map<FleetDocumentResponse>(document);
    }

    public async Task<FleetDocumentResponse?> UpdateAsync(Guid id, UpdateFleetDocumentRequest request)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document is null) return null;

        _mapper.Map(request, document);
        await _repository.UpdateAsync(document);
        await _repository.SaveChangesAsync();
        return _mapper.Map<FleetDocumentResponse>(document);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var document = await _repository.GetByIdAsync(id);
        if (document is null) return false;

        await _repository.DeleteAsync(document);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<List<FleetDocumentResponse>> GetExpiringAsync(int days) =>
        _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetExpiringAsync(days));
}
