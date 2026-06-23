using AutoMapper;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Interfaces.Fleet;
using Zorvian.Core.Entities.Fleet;
using Zorvian.Core.Interfaces;

namespace Zorvian.Application.Services.Fleet;

public sealed class DriverService
{
    private readonly IDriverRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public DriverService(IDriverRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    public async Task<List<DriverResponse>> GetAllAsync()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return [];
        var drivers = await _repo.GetAllAsync(companyId);
        return _mapper.Map<List<DriverResponse>>(drivers);
    }

    public async Task<DriverResponse?> GetByIdAsync(Guid id)
    {
        var driver = await _repo.GetByIdAsync(id);
        return driver is null ? null : _mapper.Map<DriverResponse>(driver);
    }

    public async Task<DriverResponse> CreateAsync(CreateDriverRequest request)
    {
        var driver = _mapper.Map<Driver>(request);
        driver.Status = "Active";
        await _repo.AddAsync(driver);
        await _repo.SaveChangesAsync();
        var created = await _repo.GetByIdAsync(driver.Id);
        return _mapper.Map<DriverResponse>(created!);
    }

    public async Task<DriverResponse?> UpdateAsync(Guid id, UpdateDriverRequest request)
    {
        var driver = await _repo.GetByIdAsync(id);
        if (driver is null) return null;
        _mapper.Map(request, driver);
        await _repo.SaveChangesAsync();
        return _mapper.Map<DriverResponse>(driver);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var driver = await _repo.GetByIdAsync(id);
        if (driver is null) return false;
        await _repo.DeleteAsync(driver);
        await _repo.SaveChangesAsync();
        return true;
    }
}
