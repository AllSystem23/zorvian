using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public LocationsController(ILocationRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    private Guid CompanyId => _tenant.ResolveCompanyId();

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLocationRequest request)
    {
        var location = _mapper.Map<Location>(request);
        location.CompanyId = CompanyId;
        await _repo.AddAsync(location);
        await _repo.SaveChangesAsync();
        return CreatedAtAction(null, _mapper.Map<LocationResponse>(location));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repo.GetAllAsync(CompanyId);
        return Ok(_mapper.Map<List<LocationResponse>>(items));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateLocationRequest request)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location is null) return NotFound(new { error = "Location not found" });
        _mapper.Map(request, location);
        location.CompanyId = CompanyId;
        await _repo.UpdateAsync(location);
        await _repo.SaveChangesAsync();
        return Ok(_mapper.Map<LocationResponse>(location));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var location = await _repo.GetByIdAsync(id);
        if (location is null) return NotFound(new { error = "Location not found" });
        location.IsActive = false;
        await _repo.UpdateAsync(location);
        await _repo.SaveChangesAsync();
        return NoContent();
    }
}
