using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.Vacation;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de vacaciones. Administra solicitudes, consultas, aprobación, rechazo y cálculo de saldos de vacaciones.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/vacations")]
public sealed class VacationsController : ControllerBase
{
    private readonly VacationService _service;
    private readonly IVacationRecommendationService _recommendationService;
    private readonly IVacationPaymentService _paymentService;
    private readonly IVacationReceiptPdfService _receiptPdfService;
    private readonly ITenantContext _tenant;

    public VacationsController(
        VacationService service,
        IVacationRecommendationService recommendationService,
        IVacationPaymentService paymentService,
        IVacationReceiptPdfService receiptPdfService,
        ITenantContext tenant)
    {
        _service = service;
        _recommendationService = recommendationService;
        _paymentService = paymentService;
        _receiptPdfService = receiptPdfService;
        _tenant = tenant;
    }

    /// <summary>
    /// Recomienda fechas para vacaciones basadas en disponibilidad del equipo.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
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
    [RequirePermission(Permissions.EmployeeWrite)]
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
    [RequirePermission(Permissions.EmployeeRead)]
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
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] VacationFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Obtiene las solicitudes de vacaciones del empleado autenticado.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var result = await _service.GetMyVacationsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Calcula y devuelve el saldo de vacaciones disponible del empleado autenticado.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
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
    [RequirePermission(Permissions.EmployeeWrite)]
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
    [RequirePermission(Permissions.EmployeeWrite)]
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

    // ── Vacation Payment ──────────────────────────────────────────

    /// <summary>
    /// Procesa el pago de una solicitud de vacaciones aprobada.
    /// Genera un recibo secuencial y marca la solicitud como tomada.
    /// </summary>
    [Audit("Vacation", "Pay")]
    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> PayVacation(Guid id, [FromBody] PayVacationRequest? body = null)
    {
        try
        {
            var result = await _paymentService.PayVacationAsync(
                id,
                body?.PaymentDate,
                body?.Observations,
                _tenant.CurrentUserId?.ToString());
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene el recibo de vacaciones por su ID.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("receipts/{receiptId:guid}")]
    public async Task<IActionResult> GetReceipt(Guid receiptId)
    {
        var result = await _paymentService.GetReceiptByIdAsync(receiptId);
        if (result is null)
            return NotFound(new { error = "Recibo no encontrado" });
        return Ok(result);
    }

    /// <summary>
    /// Obtiene el recibo de vacaciones para una solicitud específica.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("{id:guid}/receipt")]
    public async Task<IActionResult> GetReceiptByRequest(Guid id)
    {
        var result = await _paymentService.GetReceiptByRequestIdAsync(id);
        if (result is null)
            return NotFound(new { error = "Esta solicitud aún no tiene recibo de pago" });
        return Ok(result);
    }

    /// <summary>
    /// Lista recibos de vacaciones pagados.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("receipts")]
    public async Task<IActionResult> GetReceipts([FromQuery] Guid? employeeId, [FromQuery] int? year, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _paymentService.GetReceiptsAsync(employeeId, year, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Genera el PDF de un recibo de vacaciones.
    /// </summary>
    [RequirePermission(Permissions.EmployeeRead)]
    [HttpGet("receipts/{receiptId:guid}/pdf")]
    public async Task<IActionResult> DownloadReceiptPdf(Guid receiptId)
    {
        try
        {
            var pdfBytes = await _receiptPdfService.GenerateReceiptPdfAsync(receiptId);
            return File(pdfBytes, "application/pdf", $"recibo_vacaciones_{receiptId}.pdf");
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request body for processing a vacation payment.
/// </summary>
public sealed record PayVacationRequest(DateOnly? PaymentDate, string? Observations);
