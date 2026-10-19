using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Tax;
using Zorvian.Application.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Configuración fiscal por país. La tabla combina un CATÁLOGO GLOBAL (una fila por
/// país, compartida por todas las compañías) con AJUSTES POR COMPAÑÍA (cada empresa
/// puede redefinir las tasas de su país sin afectar a las demás).
/// </summary>
/// <remarks>
/// Catálogo global: la lectura queda abierta a quien ya usa contabilidad/nómina, pero
/// la ESCRITURA exige <see cref="Permissions.CompanyManage"/> (plataforma = SuperAdmin);
/// con <see cref="Permissions.AccountingWrite"/> cualquier CompanyAdmin podía reescribir
/// las tasas de todos los tenants. Los endpoints <c>my-country</c> son el camino para
/// que la compañía ajuste SU país: escriben una fila con su tenant y exigen
/// <see cref="Permissions.SettingsWrite"/>.
/// </remarks>
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

    // ── Ajuste de la compañía (CompanyAdmin) ──────────────────────────────────

    /// <summary>
    /// Configuración fiscal del país de la compañía: valores EFECTIVOS (su ajuste si
    /// existe, si no el catálogo global) e indicador de si son propios.
    /// </summary>
    [HttpGet("my-country")]
    [RequirePermission(Permissions.SettingsRead)]
    public async Task<IActionResult> GetMyCountry()
    {
        var item = await _service.GetMyCountryAsync();
        if (item is null)
            return NotFound(new { error = "El ajuste fiscal por compañía requiere una compañía seleccionada." });
        return Ok(item);
    }

    /// <summary>
    /// Guarda el ajuste fiscal de la compañía para su país. Crea la fila propia a
    /// partir del catálogo global la primera vez (los campos omitidos se heredan) y
    /// nunca modifica la fila global ni la de otra compañía.
    /// </summary>
    [Audit("CountryTaxConfig", "CompanyUpsert")]
    [HttpPut("my-country")]
    [RequirePermission(Permissions.SettingsWrite)]
    public async Task<IActionResult> UpsertMyCountry([FromBody] UpdateCountryTaxConfigRequest request)
    {
        try
        {
            var item = await _service.UpsertMyCountryAsync(request);
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Descarta el ajuste propio de la compañía: vuelve a usar el catálogo global.
    /// </summary>
    [Audit("CountryTaxConfig", "CompanyReset")]
    [HttpDelete("my-country")]
    [RequirePermission(Permissions.SettingsWrite)]
    public async Task<IActionResult> ResetMyCountry([FromQuery] string? countryCode)
    {
        var deleted = await _service.ResetMyCountryAsync(countryCode);
        if (!deleted)
            return NotFound(new { error = "La compañía no tiene un ajuste fiscal propio que restablecer." });
        return NoContent();
    }

    /// <summary>
    /// Crea una nueva configuración fiscal por país.
    /// </summary>
    [Audit("CountryTaxConfig", "Create")]
    [HttpPost]
    [RequirePermission(Permissions.CompanyManage)]
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
    [RequirePermission(Permissions.CompanyManage)]
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
    [RequirePermission(Permissions.CompanyManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Configuración fiscal no encontrada" });
        return NoContent();
    }
}
