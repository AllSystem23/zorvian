using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.SubscriptionPlan;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de administración de planes de suscripción y precios por empresa.
/// Solo SuperAdmin puede gestionar planes y asignar precios personalizados.
/// </summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("zorvian/v1/subscription-plans")]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly SubscriptionPlanService _service;

    public SubscriptionPlansController(SubscriptionPlanService service) => _service = service;

    // ── Plan CRUD ──

    /// <summary>
    /// Lista todos los planes de suscripción.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
    {
        var plans = activeOnly
            ? await _service.GetActivePlansAsync()
            : await _service.GetAllPlansAsync();
        return Ok(plans);
    }

    /// <summary>
    /// Obtiene un plan por ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _service.GetPlanByIdAsync(id);
        if (plan is null) return NotFound(new { error = "Plan no encontrado" });
        return Ok(plan);
    }

    /// <summary>
    /// Crea un nuevo plan de suscripción.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionPlanRequest request)
    {
        try
        {
            var plan = await _service.CreatePlanAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un plan de suscripción existente.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSubscriptionPlanRequest request)
    {
        var plan = await _service.UpdatePlanAsync(id, request);
        if (plan is null) return NotFound(new { error = "Plan no encontrado" });
        return Ok(plan);
    }

    /// <summary>
    /// Elimina un plan de suscripción.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeletePlanAsync(id);
        if (!deleted) return NotFound(new { error = "Plan no encontrado" });
        return NoContent();
    }

    // ── Per-company pricing ──

    /// <summary>
    /// Lista todos los precios personalizados por empresa.
    /// </summary>
    [HttpGet("pricing")]
    public async Task<IActionResult> GetAllPricing()
    {
        var pricing = await _service.GetAllPricingAsync();
        return Ok(pricing);
    }

    /// <summary>
    /// Lista los precios personalizados de una empresa.
    /// </summary>
    [HttpGet("pricing/company/{companyId:guid}")]
    public async Task<IActionResult> GetPricingByCompany(Guid companyId)
    {
        var pricing = await _service.GetPricingByCompanyAsync(companyId);
        return Ok(pricing);
    }

    /// <summary>
    /// Resuelve el precio efectivo para una empresa+plan (fusiona defaults con override).
    /// </summary>
    [HttpGet("pricing/resolve")]
    public async Task<IActionResult> ResolvePricing(
        [FromQuery] Guid companyId,
        [FromQuery] string planId)
    {
        var resolved = await _service.ResolvePricingAsync(companyId, planId);
        if (resolved is null) return NotFound(new { error = "Plan no encontrado" });
        return Ok(resolved);
    }

    /// <summary>
    /// Crea un precio personalizado para una empresa.
    /// </summary>
    [HttpPost("pricing")]
    public async Task<IActionResult> CreatePricing([FromBody] CreateCompanyPlanPricingRequest request)
    {
        try
        {
            var pricing = await _service.CreatePricingAsync(request);
            return CreatedAtAction(nameof(GetPricingById), new { id = pricing.Id }, pricing);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene un registro de precio personalizado por ID.
    /// </summary>
    [HttpGet("pricing/{id:guid}")]
    public async Task<IActionResult> GetPricingById(Guid id)
    {
        var pricing = await _service.GetPricingByIdAsync(id);
        if (pricing is null) return NotFound(new { error = "Registro no encontrado" });
        return Ok(pricing);
    }

    /// <summary>
    /// Actualiza un precio personalizado.
    /// </summary>
    [HttpPut("pricing/{id:guid}")]
    public async Task<IActionResult> UpdatePricing(Guid id, [FromBody] UpdateCompanyPlanPricingRequest request)
    {
        var pricing = await _service.UpdatePricingAsync(id, request);
        if (pricing is null) return NotFound(new { error = "Registro no encontrado" });
        return Ok(pricing);
    }

    /// <summary>
    /// Elimina un precio personalizado.
    /// </summary>
    [HttpDelete("pricing/{id:guid}")]
    public async Task<IActionResult> DeletePricing(Guid id)
    {
        var deleted = await _service.DeletePricingAsync(id);
        if (!deleted) return NotFound(new { error = "Registro no encontrado" });
        return NoContent();
    }
}
