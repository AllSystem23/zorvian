using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class FleetDocumentService
{
    private readonly IFleetDocumentRepository _repository;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public FleetDocumentService(IFleetDocumentRepository repository, ITenantContext tenant, IMapper mapper)
    {
        _repository = repository;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<FleetDocumentResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        return _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetAllAsync(companyId));
    }

    public async Task<List<FleetDocumentResponse>> GetByEntityAsync(string entityType, Guid entityId) =>
        _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetByEntityAsync(entityType, entityId));

    public async Task<FleetDocumentResponse?> GetByIdAsync(Guid id) =>
        _mapper.Map<FleetDocumentResponse?>(await _repository.GetByIdAsync(id));

    public async Task<FleetDocumentResponse> CreateAsync(CreateFleetDocumentRequest request)
    {
        var document = _mapper.Map<FleetDocument>(request);
        await _repository.AddAsync(document);
        await _repository.SaveChangesAsync();
        var created = await _repository.GetByIdAsync(document.Id);
        return _mapper.Map<FleetDocumentResponse>(created!);
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

    public async Task<List<FleetDocumentResponse>> GetExpiringAsync(int days)
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        return _mapper.Map<List<FleetDocumentResponse>>(await _repository.GetExpiringAsync(days, companyId));
    }
}
