using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.FixedAssets;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/fixed-assets")]
public sealed class FixedAssetsController : ControllerBase
{
    private readonly FixedAssetService _service;

    public FixedAssetsController(FixedAssetService service)
    {
        _service = service;
    }

    [Audit("FixedAsset", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFixedAssetRequest request)
    {
        try
        {
            var asset = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = asset.Id }, asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var asset = await _service.GetByIdAsync(id);
        if (asset is null)
            return NotFound(new { error = "Asset not found" });
        return Ok(asset);
    }

    [Audit("FixedAsset", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFixedAssetRequest request)
    {
        try
        {
            var asset = await _service.UpdateAsync(id, request);
            return Ok(asset);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFiltered([FromQuery] FixedAssetFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("FixedAsset", "Depreciate")]
    [HttpPost("{id:guid}/depreciate")]
    public async Task<IActionResult> RunDepreciation(Guid id, [FromBody] RunDepreciationRequest request)
    {
        try
        {
            var entry = await _service.RunDepreciationAsync(id, request);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("FixedAsset", "BulkDepreciate")]
    [HttpPost("depreciate-bulk")]
    public async Task<IActionResult> RunBulkDepreciation([FromBody] RunDepreciationRequest request)
    {
        var count = await _service.RunBulkDepreciationAsync(request.PeriodDate);
        return Ok(new { depreciated = count });
    }

    [Audit("FixedAsset", "Revalue")]
    [HttpPost("{id:guid}/revalue")]
    public async Task<IActionResult> Revalue(Guid id, [FromBody] RevalueAssetRequest request)
    {
        try
        {
            var revaluation = await _service.RevalueAsync(id, request);
            return Ok(revaluation);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("FixedAsset", "Dispose")]
    [HttpPost("{id:guid}/dispose")]
    public async Task<IActionResult> Dispose(Guid id, [FromBody] DisposeAssetRequest request)
    {
        try
        {
            var disposal = await _service.DisposeAsync(id, request);
            return Ok(disposal);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("FixedAsset", "AddMaintenance")]
    [HttpPost("{id:guid}/maintenance")]
    public async Task<IActionResult> AddMaintenance(Guid id, [FromBody] AddMaintenanceRequest request)
    {
        try
        {
            var maintenance = await _service.AddMaintenanceAsync(id, request);
            return CreatedAtAction(null, maintenance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/depreciation-schedule")]
    public async Task<IActionResult> GetDepreciationSchedule(Guid id)
    {
        try
        {
            var schedule = await _service.GetDepreciationScheduleAsync(id);
            return Ok(schedule);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _service.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpGet("maintenance/upcoming")]
    public async Task<IActionResult> GetUpcomingMaintenance([FromQuery] int days = 30)
    {
        var items = await _service.GetUpcomingMaintenanceAsync(days);
        return Ok(items);
    }
}
