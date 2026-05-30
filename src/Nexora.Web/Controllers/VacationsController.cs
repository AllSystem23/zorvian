using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Common;
using Nexora.Application.DTOs.Vacation;
using Nexora.Application.Services;
using Nexora.Web.Filters;

namespace Nexora.Web.Controllers;

/// <summary>
/// Controlador de vacaciones. Administra solicitudes, consultas, aprobación, rechazo y cálculo de saldos de vacaciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/vacations")]
public sealed class VacationsController : ControllerBase
{
    private readonly VacationService _service;

    public VacationsController(VacationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Crea una nueva solicitud de vacaciones.
    /// </summary>
    [Audit("Vacation", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVacationRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene una solicitud de vacaciones por su identificador.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null)
            return NotFound(new { error = "Vacation request not found" });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene una lista filtrada de solicitudes de vacaciones.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] VacationFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las solicitudes de vacaciones del empleado autenticado.
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetMyVacationsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Calcula y devuelve el saldo de vacaciones disponible del empleado autenticado.
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> MyBalance()
    {
        try
        {
            var result = await _service.CalculateBalanceAsync();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Aprueba una solicitud de vacaciones. Requiere comentario opcional.
    /// </summary>
    [Audit("Vacation", "Approve")]
    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] CommentRequest body)
    {
        try
        {
            var result = await _service.ApproveAsync(id, body.Comment);
            if (result is null)
                return NotFound(new { error = "Vacation request not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Rechaza una solicitud de vacaciones con un comentario.
    /// </summary>
    [Audit("Vacation", "Reject")]
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] CommentRequest body)
    {
        try
        {
            var result = await _service.RejectAsync(id, body.Comment ?? "");
            if (result is null)
                return NotFound(new { error = "Vacation request not found" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
