using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class DriverLicenseCategoryService
{
    private readonly IDriverLicenseCategoryRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public DriverLicenseCategoryService(IDriverLicenseCategoryRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<DriverLicenseCategoryResponse>> GetAllAsync(string? countryCode = null)
    {
        var categories = await _repo.GetAllAsync(countryCode);
        return _mapper.Map<List<DriverLicenseCategoryResponse>>(categories);
    }

    public async Task<DriverLicenseCategoryResponse?> GetByIdAsync(Guid id)
    {
        var category = await _repo.GetByIdAsync(id);
        return category is null ? null : _mapper.Map<DriverLicenseCategoryResponse>(category);
    }

    public async Task<DriverLicenseCategoryResponse> CreateAsync(CreateDriverLicenseCategoryRequest request)
    {
        var category = _mapper.Map<DriverLicenseCategory>(request);
        await _repo.AddAsync(category);
        await _repo.SaveChangesAsync();
        return _mapper.Map<DriverLicenseCategoryResponse>(category);
    }

    public async Task<DriverLicenseCategoryResponse?> UpdateAsync(Guid id, UpdateDriverLicenseCategoryRequest request)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return null;
        _mapper.Map(request, category);
        await _repo.SaveChangesAsync();
        return _mapper.Map<DriverLicenseCategoryResponse>(category);
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
