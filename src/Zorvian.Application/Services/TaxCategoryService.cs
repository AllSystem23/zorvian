using AutoMapper;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class TaxCategoryService
{
    private readonly ITaxCategoryRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public TaxCategoryService(ITaxCategoryRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo; _tenant = tenant; _mapper = mapper;
    }

    private Guid CompanyId => _tenant.RequireCompanyId();

    public async Task<List<TaxCategoryResponse>> GetAllAsync()
    {
        var items = await _repo.GetByCompanyIdAsync(CompanyId);
        return _mapper.Map<List<TaxCategoryResponse>>(items);
    }

    public async Task<TaxCategoryResponse?> GetByIdAsync(Guid id)
    {
        var item = await _repo.GetByIdAsync(id);
        return item is null ? null : _mapper.Map<TaxCategoryResponse>(item);
    }

    public async Task<TaxCategoryResponse> CreateAsync(CreateTaxCategoryRequest request)
    {
        var entity = _mapper.Map<TaxCategory>(request);
        entity.CompanyId = CompanyId;
        await _repo.AddAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<TaxCategoryResponse>(entity);
    }

    public async Task<TaxCategoryResponse?> UpdateAsync(Guid id, UpdateTaxCategoryRequest request)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null) return null;
        _mapper.Map(request, entity);
        await _repo.UpdateAsync(entity);
        await _repo.SaveChangesAsync();
        return _mapper.Map<TaxCategoryResponse>(entity);
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
