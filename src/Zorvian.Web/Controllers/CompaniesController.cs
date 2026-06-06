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
