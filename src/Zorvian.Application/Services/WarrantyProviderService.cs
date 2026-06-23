using AutoMapper;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantyProviderService
{
    private readonly IWarrantyProviderRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public WarrantyProviderService(IWarrantyProviderRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<WarrantyProviderResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var providers = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<WarrantyProviderResponse>>(providers);
    }

    public async Task<WarrantyProviderResponse?> GetByIdAsync(Guid id)
    {
        var provider = await _repo.GetByIdAsync(id);
        return provider is null ? null : _mapper.Map<WarrantyProviderResponse>(provider);
    }

    public async Task<WarrantyProviderResponse> CreateAsync(CreateWarrantyProviderRequest request)
    {
        var provider = _mapper.Map<WarrantyProvider>(request);
        await _repo.AddAsync(provider);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyProviderResponse>(provider);
    }

    public async Task<WarrantyProviderResponse?> UpdateAsync(Guid id, UpdateWarrantyProviderRequest request)
    {
        var provider = await _repo.GetByIdAsync(id);
        if (provider is null) return null;

        _mapper.Map(request, provider);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantyProviderResponse>(provider);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var provider = await _repo.GetByIdAsync(id);
        if (provider is null) return false;

        await _repo.DeleteAsync(provider);
        await _repo.SaveChangesAsync();
        return true;
    }
}
