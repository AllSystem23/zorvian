using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Branch;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/branches")]
public sealed class BranchesController : ControllerBase
{
    private readonly BranchService _service;

    public BranchesController(BranchService service)
    {
        _service = service;
    }

    // LECTURA abierta a cualquier usuario autenticado de la compañía: la lista
    // de sucursales alimenta el selector del cabecero (todas las páginas) y el
    // `HasQueryFilter` de `Branch` la acota a la compañía del contexto. Antes
    // exigía `company.manage` (permiso de PLATAFORMA, solo SuperAdmin) y el 403
    // dejaba el selector de sucursales vacío para CompanyAdmin/Supervisor/RRHH/
    // Empleado sin ningún aviso (el provider traga el error con `catch → []`).
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var branches = await _service.GetAllAsync();
        return Ok(branches);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var branch = await _service.GetByIdAsync(id);
        if (branch is null)
            return NotFound(new { error = "Branch not found" });
        return Ok(branch);
    }

    [Audit("Branch", "Create")]
    [RequirePermission(Permissions.SettingsWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchRequest request)
    {
        var branch = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
    }

    [Audit("Branch", "Update")]
    [RequirePermission(Permissions.SettingsWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchRequest request)
    {
        var branch = await _service.UpdateAsync(id, request);
        if (branch is null)
            return NotFound(new { error = "Branch not found" });
        return Ok(branch);
    }

    [Audit("Branch", "Delete")]
    [RequirePermission(Permissions.SettingsWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Branch not found" });
        return NoContent();
    }
}
