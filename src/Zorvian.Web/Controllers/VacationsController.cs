using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Vacation;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de vacaciones. Administra solicitudes, consultas, aprobación, rechazo y cálculo de saldos de vacaciones.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/vacations")]
public sealed class VacationsController : ControllerBase
{
    private readonly VacationService _service;
    private readonly IVacationRecommendationService _recommendationService;
    private readonly ITenantContext _tenant;

    public VacationsController(VacationService service, IVacationRecommendationService recommendationService, ITenantContext tenant)
    {
        _service = service;
        _recommendationService = recommendationService;
        _tenant = tenant;
    }

    /// <summary>
    /// Recomienda fechas para vacaciones basadas en disponibilidad del equipo.
    /// </summary>
    [HttpGet("recommend")]
    public async Task<IActionResult> Recommend([FromQuery] int days, [FromQuery] int month, [FromQuery] int year)
    {
        if (_tenant.CurrentEmployeeId is null)
            return Unauthorized();
            
        var result = await _recommendationService.RecommendDatesAsync(_tenant.CurrentEmployeeId.Value, days, month, year);
        return Ok(result);
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
