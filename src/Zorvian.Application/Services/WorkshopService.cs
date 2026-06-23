using AutoMapper;
using Zorvian.Application.DTOs.Warranty;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services;

public sealed class WorkshopService
{
    private readonly IServiceWorkshopRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public WorkshopService(IServiceWorkshopRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<ServiceWorkshopResponse>> GetAllAsync()
    {
        var companyId = Guid.Parse(_tenant.TenantId);
        var workshops = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<ServiceWorkshopResponse>>(workshops);
    }

    public async Task<ServiceWorkshopResponse?> GetByIdAsync(Guid id)
    {
        var workshop = await _repo.GetByIdAsync(id);
        return workshop is null ? null : _mapper.Map<ServiceWorkshopResponse>(workshop);
    }

    public async Task<ServiceWorkshopResponse> CreateAsync(CreateServiceWorkshopRequest request)
    {
        var workshop = _mapper.Map<ServiceWorkshop>(request);
        workshop.CompanyId = Guid.TryParse(_tenant.TenantId?.ToString(), out var companyId) ? companyId : Guid.Empty;
        await _repo.AddAsync(workshop);
        await _repo.SaveChangesAsync();

        return _mapper.Map<ServiceWorkshopResponse>(workshop);
    }

    public async Task<ServiceWorkshopResponse?> UpdateAsync(Guid id, UpdateServiceWorkshopRequest request)
    {
        var workshop = await _repo.GetByIdAsync(id);
        if (workshop is null) return null;

        _mapper.Map(request, workshop);
        await _repo.SaveChangesAsync();

        return _mapper.Map<ServiceWorkshopResponse>(workshop);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var workshop = await _repo.GetByIdAsync(id);
        if (workshop is null) return false;

        await _repo.DeleteAsync(workshop);
        await _repo.SaveChangesAsync();
        return true;
    }
}
