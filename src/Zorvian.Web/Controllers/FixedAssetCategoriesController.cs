using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/fixed-asset-categories")]
public sealed class FixedAssetCategoriesController : ControllerBase
{
    private readonly IFixedAssetCategoryRepository _repo;
    private readonly ITenantContext _tenant;
    private readonly IMapper _mapper;

    public FixedAssetCategoriesController(IFixedAssetCategoryRepository repo, ITenantContext tenant, IMapper mapper)
    {
        _repo = repo;
        _tenant = tenant;
        _mapper = mapper;
    }

    private Guid CompanyId => _tenant.ResolveCompanyId();

    [RequirePermission(Permissions.FixedAssetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetCategoryRequest request)
    {
        var category = _mapper.Map<FixedAssetCategory>(request);
        category.CompanyId = CompanyId;
        await _repo.AddAsync(category);
        await _repo.SaveChangesAsync();
        return CreatedAtAction(null, _mapper.Map<FixedAssetCategoryResponse>(category));
    }

    [RequirePermission(Permissions.FixedAssetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _repo.GetAllAsync(CompanyId);
        return Ok(_mapper.Map<List<FixedAssetCategoryResponse>>(items));
    }

    [RequirePermission(Permissions.FixedAssetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateFixedAssetCategoryRequest request)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return NotFound(new { error = "Category not found" });
        _mapper.Map(request, category);
        category.CompanyId = CompanyId;
        await _repo.UpdateAsync(category);
        await _repo.SaveChangesAsync();
        return Ok(_mapper.Map<FixedAssetCategoryResponse>(category));
    }

    [RequirePermission(Permissions.FixedAssetWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _repo.GetByIdAsync(id);
        if (category is null) return NotFound(new { error = "Category not found" });
        category.IsActive = false;
        await _repo.UpdateAsync(category);
        await _repo.SaveChangesAsync();
        return NoContent();
    }
}
