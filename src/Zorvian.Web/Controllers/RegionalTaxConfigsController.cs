using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de configuración fiscal regional. Gestiona las tasas impositivas por país.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/regional-tax-configs")]
public sealed class RegionalTaxConfigsController : ControllerBase
{
    private readonly IRegionalTaxConfigService _service;
    private readonly ITenantContext _tenant;

    public RegionalTaxConfigsController(IRegionalTaxConfigService service, ITenantContext tenant)
    {
        _service = service;
        _tenant = tenant;
    }

    private Guid ResolveCompanyId() => _tenant.ResolveCompanyId();

    /// <summary>
    /// Obtiene todas las configuraciones fiscales de la empresa.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync(ResolveCompanyId());
        return Ok(items);
    }

    /// <summary>
    /// Obtiene una configuración fiscal por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id, ResolveCompanyId());
        if (item is null) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return Ok(item);
    }

    /// <summary>
    /// Obtiene las tasas activas para un país específico.
    /// </summary>
    [HttpGet("by-country/{countryCode}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetByCountry(string countryCode)
    {
        var items = await _service.GetActiveTaxesAsync(countryCode, ResolveCompanyId());
        return Ok(items);
    }

    /// <summary>
    /// Crea una nueva configuración fiscal regional.
    /// </summary>
    [Audit("RegionalTaxConfig", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Create([FromBody] CreateRegionalTaxConfigRequest request)
    {
        var item = await _service.CreateAsync(request, ResolveCompanyId());
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    /// <summary>
    /// Actualiza una configuración fiscal existente.
    /// </summary>
    [Audit("RegionalTaxConfig", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRegionalTaxConfigRequest request)
    {
        var item = await _service.UpdateAsync(id, request, ResolveCompanyId());
        if (item is null) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return Ok(item);
    }

    /// <summary>
    /// Elimina una configuración fiscal.
    /// </summary>
    [Audit("RegionalTaxConfig", "Delete")]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id, ResolveCompanyId());
        if (!deleted) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return NoContent();
    }
}
