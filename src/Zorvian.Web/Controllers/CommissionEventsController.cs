using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Services;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize] // Ajustar según requisitos de seguridad de webhooks externos
[Route("zorvian/v1/commissions")]
public sealed class CommissionEventsController : ControllerBase
{
    private readonly ICommissionService _commissionService;
    private readonly ILogger<CommissionEventsController> _logger;

    public CommissionEventsController(ICommissionService commissionService, ILogger<CommissionEventsController> logger)
    {
        _commissionService = commissionService;
        _logger = logger;
    }

    /// <summary>
    /// Recibe eventos genéricos de CRM para el motor de comisiones.
    /// </summary>
    [HttpPost("events")]
    public async Task<IActionResult> HandleEvent([FromBody] CommissionEventRequest request)
    {
        _logger.LogInformation("Processing event: {EventType}", request.EventType);
        await _commissionService.ProcessEventAsync(request);
        return Ok();
    }

    /// <summary>
    /// Recibe eventos de ventas (facturas, cobranzas) para el motor de comisiones.
    /// </summary>
    [HttpPost("sales")]
    public async Task<IActionResult> HandleSale([FromBody] CommissionSaleRequest request)
    {
        await _commissionService.ProcessSaleAsync(request);
        return Ok();
    }
}
