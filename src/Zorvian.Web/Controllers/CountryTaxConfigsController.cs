using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Configuración fiscal por país. Catálogo global de configuraciones de INSS, IR, prestaciones.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/country-tax-configs")]
public sealed class CountryTaxConfigsController : ControllerBase
{
    private readonly ICountryTaxConfigService _service;

    public CountryTaxConfigsController(ICountryTaxConfigService service) => _service = service;

    /// <summary>
    /// Obtiene todas las configuraciones fiscales por país.
    /// </summary>
    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    /// <summary>
    /// Obtiene una configuración fiscal por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item is null) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return Ok(item);
    }

    /// <summary>
    /// Obtiene la configuración fiscal para un país específico por código ISO.
    /// </summary>
    [HttpGet("by-country/{countryCode}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetByCountry(string countryCode)
    {
        var item = await _service.GetByCountryCodeAsync(countryCode);
        if (item is null) return NotFound(new { error = $"No hay configuración fiscal para '{countryCode}'" });
        return Ok(item);
    }

    /// <summary>
    /// Crea una nueva configuración fiscal por país.
    /// </summary>
    [Audit("CountryTaxConfig", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Create([FromBody] CreateCountryTaxConfigRequest request)
    {
        try
        {
            var item = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza una configuración fiscal existente.
    /// </summary>
    [Audit("CountryTaxConfig", "Update")]
    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCountryTaxConfigRequest request)
    {
        try
        {
            var item = await _service.UpdateAsync(id, request);
            if (item is null) return NotFound(new { error = "Configuración fiscal no encontrada" });
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Elimina una configuración fiscal por país.
    /// </summary>
    [Audit("CountryTaxConfig", "Delete")]
    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return NoContent();
    }
}
