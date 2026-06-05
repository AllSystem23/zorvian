using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Employee;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/employees")]
public sealed class EmployeesController : ControllerBase
{
    private readonly EmployeeService _service;
    private readonly IExcelImportService _import;
    private readonly ITenantContext _tenant;

    public EmployeesController(EmployeeService service, IExcelImportService import, ITenantContext tenant)
    {
        _service = service;
        _import = import;
        _tenant = tenant;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        if (_tenant.CurrentEmployeeId is null && !isSuperAdmin)
            return Unauthorized(new { error = "No employee profile linked" });

        if (isSuperAdmin && _tenant.CurrentEmployeeId is null)
            return Ok(new { Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value, FullName = "Super Admin" });

        var employee = await _service.GetByIdAsync(_tenant.CurrentEmployeeId!.Value);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileRequest request)
    {
        var isSuperAdmin = User.IsInRole("SuperAdmin");
        if (_tenant.CurrentEmployeeId is null && !isSuperAdmin)
            return Unauthorized(new { error = "No employee profile linked" });

        if (isSuperAdmin && _tenant.CurrentEmployeeId is null)
            return BadRequest(new { error = "Cannot update profile for non-linked Super Admin" });

        var employee = await _service.UpdateMyProfileAsync(_tenant.CurrentEmployeeId!.Value, request);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpGet("me/certificate")]
    public async Task<IActionResult> GetEmploymentCertificate()
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized(new { error = "No employee profile linked" });

        var employee = await _service.GetByIdAsync(_tenant.CurrentEmployeeId.Value);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });

        var name = $"{employee.FirstName} {employee.LastName}";
        var hired = employee.HireDate.ToString("dd/MM/yyyy");

        var html = $@"<!DOCTYPE html>
<html lang=""es"">
<head><meta charset=""utf-8""><title>Constancia Laboral</title>
<style>
  body {{ font-family: 'Segoe UI', Arial, sans-serif; padding: 40px; max-width: 700px; margin: auto; }}
  h1 {{ text-align: center; color: #1e3a5f; margin-bottom: 30px; }}
  .content {{ line-height: 1.8; text-align: justify; }}
  .signature {{ margin-top: 60px; text-align: center; }}
  .footer {{ margin-top: 40px; font-size: 12px; color: #666; text-align: center; }}
</style>
</head>
<body>
  <h1 style=""text-align:center;"">CONSTANCIA LABORAL</h1>
  <p>El suscrito, representante legal de <strong>Zorvian ERP</strong>, hace constar que:</p>
  <p style=""text-align:center;font-size:18px;""><strong>{name}</strong></p>
  <p>con cédula de identidad <strong>{employee.IdentificationNumber}</strong>, labora desde el <strong>{hired}</strong> en el cargo de <strong>{employee.Position}</strong> en <strong>{employee.DepartmentName}</strong>.</p>
  <p>Estado actual: <strong>{employee.Status}</strong>.</p>
  <p>Se extiende la presente para los fines que el interesado estime conveniente.</p>
  <div class=""signature"">
    <p>_________________________________</p>
    <p><strong>Recursos Humanos</strong><br>Zorvian ERP</p>
  </div>
  <div class=""footer"">
    <p>Emitido el {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</p>
  </div>
</body>
</html>";

        Response.Headers.Append("Content-Disposition", "attachment; filename=constancia_laboral.html");
        return Content(html, "text/html");
    }

    [Audit("Employee", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request)
    {
        var employee = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = await _service.GetByIdAsync(id);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpGet]
    [RequirePermission(Permissions.EmployeeRead)]
    public async Task<IActionResult> GetList([FromQuery] EmployeeFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Employee", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.EmployeeWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        var employee = await _service.UpdateAsync(id, request);
        if (employee is null)
            return NotFound(new { error = "Employee not found" });
        return Ok(employee);
    }

    [HttpPost("import")]
    [RequirePermission(Permissions.EmployeeImport)]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Debe seleccionar un archivo Excel" });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "El archivo debe ser .xlsx" });

        using var stream = file.OpenReadStream();
        var result = await _import.ImportAsync(stream);
        return Ok(result);
    }

    [Audit("Employee", "Delete")]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.EmployeeDelete)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Employee not found" });
        return NoContent();
    }
}
