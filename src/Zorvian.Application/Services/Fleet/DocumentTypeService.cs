using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
namespace Zorvian.Application.Services.Fleet;

public sealed class DocumentTypeService
{
    private readonly IDocumentTypeRepository _repository;
    private readonly IMapper _mapper;

    public DocumentTypeService(IDocumentTypeRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<DocumentTypeResponse>> GetAllAsync() =>
        _mapper.Map<List<DocumentTypeResponse>>(await _repository.GetAllAsync());

    public async Task<List<DocumentTypeResponse>> GetByEntityTypeAsync(string entityType) =>
        _mapper.Map<List<DocumentTypeResponse>>(await _repository.GetByEntityTypeAsync(entityType));

    public async Task<DocumentTypeResponse?> GetByIdAsync(Guid id) =>
        _mapper.Map<DocumentTypeResponse?>(await _repository.GetByIdAsync(id));

    public async Task<DocumentTypeResponse> CreateAsync(CreateDocumentTypeRequest request)
    {
        var entity = _mapper.Map<DocumentType>(request);
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<DocumentTypeResponse>(entity);
    }

    public async Task<DocumentTypeResponse?> UpdateAsync(Guid id, UpdateDocumentTypeRequest request)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;

        _mapper.Map(request, entity);
        await _repository.UpdateAsync(entity);
        await _repository.SaveChangesAsync();
        return _mapper.Map<DocumentTypeResponse>(entity);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return false;

        await _repository.DeleteAsync(entity);
        await _repository.SaveChangesAsync();
        return true;
    }
}
