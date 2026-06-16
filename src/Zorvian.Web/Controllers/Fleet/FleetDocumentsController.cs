using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Fleet;
using Zorvian.Application.Services.Fleet;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers.Fleet;

[ApiController]
[Authorize]
[Route("zorvian/v1/fleet/documents")]
public sealed class FleetDocumentsController : ControllerBase
{
    private readonly FleetDocumentService _service;

    public FleetDocumentsController(FleetDocumentService service) => _service = service;

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var documents = await _service.GetAllAsync();
        return Ok(new { items = documents });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetByEntity(string entityType, Guid entityId)
    {
        var documents = await _service.GetByEntityAsync(entityType, entityId);
        return Ok(new { items = documents });
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _service.GetByIdAsync(id);
        if (document is null) return NotFound(new { error = "Document not found" });
        return Ok(document);
    }

    [Audit("FleetDocument", "Create")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFleetDocumentRequest request)
    {
        var document = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    [Audit("FleetDocument", "Update")]
    [RequirePermission(Permissions.FleetWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFleetDocumentRequest request)
    {
        var document = await _service.UpdateAsync(id, request);
        if (document is null) return NotFound(new { error = "Document not found" });
        return Ok(document);
    }

    [Audit("FleetDocument", "Delete")]
    [RequirePermission(Permissions.FleetDelete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Document not found" });
        return NoContent();
    }

    [RequirePermission(Permissions.FleetRead)]
    [HttpGet("expiring/{days:int}")]
    public async Task<IActionResult> GetExpiring(int days)
    {
        var documents = await _service.GetExpiringAsync(days);
        return Ok(new { items = documents });
    }
}
