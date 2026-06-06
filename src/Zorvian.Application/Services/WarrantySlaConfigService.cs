using AutoMapper;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WarrantySlaConfigService
{
    private readonly IWarrantySlaConfigRepository _repo;
    private readonly IMapper _mapper;
    private readonly ITenantContext _tenant;

    public WarrantySlaConfigService(
        IWarrantySlaConfigRepository repo,
        IMapper mapper,
        ITenantContext tenant)
    {
        _repo = repo;
        _mapper = mapper;
        _tenant = tenant;
    }

    public async Task<List<WarrantySlaConfigResponse>> GetAllAsync()
    {
        var configs = await _repo.GetAllAsync();
        return _mapper.Map<List<WarrantySlaConfigResponse>>(configs);
    }

    public async Task<WarrantySlaConfigResponse?> GetByIdAsync(Guid id)
    {
        var config = await _repo.GetByIdAsync(id);
        return config is null ? null : _mapper.Map<WarrantySlaConfigResponse>(config);
    }

    public async Task<WarrantySlaConfigResponse> CreateAsync(CreateWarrantySlaConfigRequest request)
    {
        var config = _mapper.Map<WarrantySlaConfig>(request);
        config.TenantId = _tenant.TenantId;
        config.CompanyId = Guid.TryParse(_tenant.TenantId, out var companyId) ? companyId : Guid.Empty;
        
        await _repo.AddAsync(config);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantySlaConfigResponse>(config);
    }

    public async Task<WarrantySlaConfigResponse?> UpdateAsync(Guid id, UpdateWarrantySlaConfigRequest request)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config is null) return null;

        _mapper.Map(request, config);
        await _repo.SaveChangesAsync();

        return _mapper.Map<WarrantySlaConfigResponse>(config);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var config = await _repo.GetByIdAsync(id);
        if (config is null) return false;

        await _repo.DeleteAsync(config);
        await _repo.SaveChangesAsync();
        return true;
    }
}
