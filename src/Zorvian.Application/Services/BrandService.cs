using AutoMapper;
using Zorvian.Application.DTOs.Inventory;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class BrandService
{
    private readonly IBrandRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public BrandService(IBrandRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<BrandResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var brands = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<BrandResponse>>(brands);
    }

    public async Task<BrandResponse> CreateAsync(CreateBrandRequest request)
    {
        var brand = _mapper.Map<Brand>(request);
        brand.CompanyId = Guid.Parse(_tenant.TenantId);

        await _repo.AddAsync(brand);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BrandResponse>(brand);
    }

    public async Task<BrandResponse?> UpdateAsync(Guid id, UpdateBrandRequest request)
    {
        var brand = await _repo.GetByIdAsync(id);
        if (brand is null) return null;

        _mapper.Map(request, brand);
        await _repo.SaveChangesAsync();

        return _mapper.Map<BrandResponse>(brand);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var brand = await _repo.GetByIdAsync(id);
        if (brand is null) return false;

        await _repo.DeleteAsync(brand);
        await _repo.SaveChangesAsync();
        return true;
    }
}
