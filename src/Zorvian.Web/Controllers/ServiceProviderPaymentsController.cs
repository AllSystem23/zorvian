using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Zorvian.Application.DTOs.Common;
using Zorvian.Application.DTOs.ServiceProviderPayment;
using Zorvian.Application.Interfaces;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Pago a Prestadores de Servicios (colaborador NO de planilla): pago quincenal por
/// recaudo con comisión porcentual, incentivo por meta y deducciones con acuerdo previo.
/// Edición manual en borrador e historial por agente/período.
/// </summary>
[Authorize]
[ApiController]
[Route("zorvian/v1/service-provider-payments")]
public sealed class ServiceProviderPaymentsController : ControllerBase
{
    private readonly ServiceProviderPaymentService _service;
    private readonly IDocumentTemplateRepository _templateRepo;
    private readonly IDocumentService _documentService;
    private readonly IDocumentGenerationService _documentGenerationService;
    private readonly ILogger<ServiceProviderPaymentsController> _logger;

    public ServiceProviderPaymentsController(
        ServiceProviderPaymentService service,
        IDocumentTemplateRepository templateRepo,
        IDocumentService documentService,
        IDocumentGenerationService documentGenerationService,
        ILogger<ServiceProviderPaymentsController> logger)
    {
        _service = service;
        _templateRepo = templateRepo;
        _documentService = documentService;
        _documentGenerationService = documentGenerationService;
        _logger = logger;
    }

    [RequirePermission(Permissions.PayrollRead)]
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] Guid? employeeId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] int? half,
        [FromQuery] string? status,
        [FromQuery] Guid? branchId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, total) = await _service.GetFilteredAsync(
            search, employeeId, year, month, half, status, branchId, page, pageSize);
        return Ok(new PagedResult<ServiceProviderPaymentResponse>(items, total, page, pageSize));
    }

    [RequirePermission(Permissions.PayrollRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound(new { detail = "Pago no encontrado" });
        return Ok(result);
    }

    [Audit("ServiceProviderPayment", "Create")]
    [RequirePermission(Permissions.PayrollWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceProviderPaymentRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [Audit("ServiceProviderPayment", "Update")]
    [RequirePermission(Permissions.PayrollWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceProviderPaymentRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [Audit("ServiceProviderPayment", "Approve")]
    [RequirePermission(Permissions.PayrollProcess)]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            return Ok(await _service.ApproveAsync(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [Audit("ServiceProviderPayment", "MarkPaid")]
    [RequirePermission(Permissions.PayrollProcess)]
    [HttpPost("{id:guid}/pay")]
    public async Task<IActionResult> MarkPaid(Guid id, [FromBody] MarkServiceProviderPaymentPaidRequest? request)
    {
        try
        {
            return Ok(await _service.MarkPaidAsync(id, request ?? new MarkServiceProviderPaymentPaidRequest(null)));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    [Audit("ServiceProviderPayment", "Cancel")]
    [RequirePermission(Permissions.PayrollWrite)]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            return Ok(await _service.CancelAsync(id));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { detail = ex.Message });
        }
    }

    /// <summary>Comprobante de Pago (PDF) vía el motor documental.</summary>
    [Audit("ServiceProviderPayment", "GeneratePdf")]
    [RequirePermission(Permissions.PayrollRead)]
    [HttpPost("{id:guid}/pdf/generate")]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        try
        {
            var context = await _documentGenerationService.GetEntityContextAsync("servicepayment", id);
            if (context is null)
                return NotFound(new { error = "Pago no encontrado" });

            var template = await _templateRepo.GetByNameAsync("Pago a Prestador de Servicios");
            if (template is null)
                return NotFound(new { error = "Plantilla 'Pago a Prestador de Servicios' no encontrada. Ejecute el seed." });

            var generated = await _documentService.GenerateProfessionalDocumentAsync(
                template.Id,
                id,
                context.Data,
                "servicepayment");

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
            _logger.LogError(ex, "Error generando comprobante de pago a prestador {PaymentId}", id);
            return StatusCode(500, new { error = "Error interno al generar el documento" });
        }
    }
}
