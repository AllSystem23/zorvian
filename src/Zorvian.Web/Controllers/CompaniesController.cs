using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Company;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de empresas. Administra la configuración de la compañía y sus ajustes generales.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/companies")]
public sealed class CompaniesController : ControllerBase
{
    private readonly CompanyService _companyService;

    public CompaniesController(CompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// Lista todas las empresas (solo SuperAdmin).
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll()
    {
        var companies = await _companyService.GetAllAsync();
        return Ok(companies);
    }

    /// <summary>
    /// Actualiza cualquier empresa por ID (solo SuperAdmin).
    /// </summary>
    [Audit("Company", "AdminUpdate")]
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AdminUpdate(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var result = await _companyService.UpdateByIdAsync(id, request);
        if (result is null)
            return NotFound(new { error = "Empresa no encontrada" });
        return Ok(result);
    }

    /// <summary>
    /// Sube o reemplaza el logo de una empresa.
    /// </summary>
    [Audit("Company", "UploadLogo")]
    [HttpPost("{id:guid}/logo")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No se proporcionó un archivo" });

        var allowedTypes = new[] { "image/png", "image/jpeg", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Solo se permiten archivos PNG, JPG o WebP" });

        await using var stream = file.OpenReadStream();
        var url = await _companyService.UploadLogoAsync(id, stream, file.ContentType);

        if (url is null)
            return NotFound(new { error = "Empresa no encontrada" });

        return Ok(new { logoUrl = url });
    }

    /// <summary>
    /// Desactiva una empresa (solo SuperAdmin).
    /// </summary>
    [Audit("Company", "AdminDeactivate")]
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AdminDeactivate(Guid id)
    {
        try
        {
            var result = await _companyService.DeactivateAsync(id);
            if (!result)
                return NotFound(new { error = "Empresa no encontrada" });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Crea una nueva empresa en el sistema.
    /// </summary>
    [HttpPost]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        try
        {
            var company = await _companyService.CreateAsync(request);
            return CreatedAtAction(nameof(GetCurrent), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene la información de la empresa actual.
    /// </summary>
    [HttpGet("current")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> GetCurrent()
    {
        var company = await _companyService.GetCurrentAsync();
        if (company is null)
            return NotFound(new { error = "Company not configured" });
        return Ok(company);
    }

    /// <summary>
    /// Actualiza los datos de la empresa actual.
    /// </summary>
    [Audit("Company", "Update")]
    [HttpPut("current")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> Update([FromBody] UpdateCompanyRequest request)
    {
        var result = await _companyService.UpdateAsync(request);
        if (result is null)
            return NotFound(new { error = "Company not configured" });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene la configuración general de la empresa.
    /// </summary>
    [HttpGet("settings")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _companyService.GetSettingsAsync();
        if (settings is null)
            return NotFound(new { error = "Settings not configured" });
        return Ok(settings);
    }

    /// <summary>
    /// Actualiza la configuración general de la empresa.
    /// </summary>
    [Audit("Company", "UpdateSettings")]
    [HttpPut("settings")]
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateCompanySettingsRequest request)
    {
        var result = await _companyService.UpdateSettingsAsync(request);
        if (result is null)
            return NotFound(new { error = "Company not configured" });
        return Ok(result);
    }
}
