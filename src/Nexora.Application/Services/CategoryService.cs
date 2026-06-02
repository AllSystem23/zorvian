using AutoMapper;
using Nexora.Application.DTOs.Inventory;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Core.Interfaces;

namespace Nexora.Application.Services;

public sealed class CategoryService
{
    private readonly ICategoryRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public CategoryService(ICategoryRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<CategoryResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var categories = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<CategoryResponse>>(categories);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var category = _mapper.Map<Category>(request);
        category.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(category);
        await _repo.SaveChangesAsync();

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(Guid id, UpdateCategoryRequest request)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return null;

        _mapper.Map(request, category);
        await _repo.SaveChangesAsync();

        return _mapper.Map<CategoryResponse>(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return false;

        await _repo.DeleteAsync(category);
        await _repo.SaveChangesAsync();
        return true;
    }
}
