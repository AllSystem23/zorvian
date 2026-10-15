using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.CashRegister;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/cash-registers")]
public sealed class CashRegistersController : ControllerBase
{
    private readonly CashRegisterService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<CashRegistersController> _logger;

    public CashRegistersController(
        CashRegisterService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<CashRegistersController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [Audit("CashRegister", "Create")]
    [RequirePermission(Permissions.CashWrite)]
    [HttpPost("open")]
    public async Task<IActionResult> Open([FromBody] OpenCashRegisterRequest request)
    {
        try
        {
            var register = await _service.OpenAsync(request);
            return Ok(register);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("CashRegister", "Close")]
    [RequirePermission(Permissions.CashWrite)]
    [HttpPost("{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseCashRegisterRequest request)
    {
        try
        {
            var register = await _service.CloseAsync(id, request);
            return Ok(register);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.CashRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var register = await _service.GetByIdAsync(id);
        if (register is null)
            return NotFound(new { error = "Cash register not found" });
        return Ok(register);
    }

    [RequirePermission(Permissions.CashRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] CashRegisterFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("CashMovement", "Create")]
    [RequirePermission(Permissions.CashWrite)]
    [HttpPost("{id:guid}/movements")]
    public async Task<IActionResult> AddMovement(Guid id, [FromBody] CreateCashMovementRequest request)
    {
        try
        {
            var movement = await _service.AddMovementAsync(request with { CashRegisterId = id });
            return Ok(movement);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.CashRead)]
    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid id)
    {
        var movements = await _service.GetMovementsAsync(id);
        return Ok(movements);
    }

    [Audit("CashRegister", "Arqueo")]
    [RequirePermission(Permissions.CashWrite)]
    [HttpPost("{id:guid}/arqueo")]
    public async Task<IActionResult> CreateArqueo(Guid id, [FromBody] CreateArqueoRequest request)
    {
        try
        {
            var result = await _service.CreateArqueoAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("CashRegister", "ReadArqueo")]
    [RequirePermission(Permissions.CashRead)]
    [HttpGet("{id:guid}/arqueo")]
    public async Task<IActionResult> GetArqueo(Guid id)
    {
        var result = await _service.GetArqueoAsync(id);
        if (result is null)
            return NotFound(new { error = "Arqueo not found" });
        return Ok(result);
    }

    [Audit("CashMovement", "GeneratePdf")]
    [RequirePermission(Permissions.CashRead)]
    [HttpPost("movements/{movementId:guid}/pdf/generate")]
    public async Task<IActionResult> GenerateMovementPdf(Guid movementId)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("cashmovement", movementId);
            if (context is null)
                return NotFound(new { error = "Movimiento de caja no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Comprobante de Caja");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Comprobante de Caja' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                movementId,
                context.Data,
                "cashmovement");

            return Ok(new
            {
                generatedDocumentId = generated.Id,
                name = generated.Name,
                status = generated.Status,
                createdAt = generated.CreatedAt,
                downloadUrl = $"/zorvian/v1/documents/{generated.Id}/pdf"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando comprobante de caja {MovementId}", movementId);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
