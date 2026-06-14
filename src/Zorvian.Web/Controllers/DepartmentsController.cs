using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Department;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de departamentos. Administra la creación, consulta, actualización y eliminación de departamentos.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly DepartmentService _service;

    public DepartmentsController(DepartmentService service)
    {
        _service = service;
    }

    /// <summary>
    /// Crea un nuevo departamento.
    /// </summary>
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    /// <summary>
    /// Obtiene un departamento por su identificador único.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dept = await _service.GetByIdAsync(id);
        if (dept is null)
            return NotFound(new { error = "Department not found" });
        return Ok(dept);
    }

    /// <summary>
    /// Obtiene todos los departamentos registrados.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _service.GetAllAsync();
        return Ok(departments);
    }

    /// <summary>
    /// Actualiza los datos de un departamento existente.
    /// </summary>
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var dept = await _service.UpdateAsync(id, request);
        if (dept is null)
            return NotFound(new { error = "Department not found" });
        return Ok(dept);
    }

    /// <summary>
    /// Elimina un departamento. Devuelve conflicto si tiene empleados asociados.
    /// </summary>
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var (success, error) = await _service.DeleteAsync(id);
        if (!success)
            return error == "Department not found"
                ? NotFound(new { error })
                : Conflict(new { error });
        return NoContent();
    }
}
