using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class ElectronicInvoiceController : ControllerBase
{
    private readonly IElectronicInvoiceService _service;

    public ElectronicInvoiceController(IElectronicInvoiceService service)
    {
        _service = service;
    }

    [HttpPost("issue")]
    public async Task<IActionResult> Issue([FromBody] IssueInvoiceRequest request)
    {
        try
        {
            var result = await _service.IssueAsync(request.SaleId, request.CountryCode);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("sale/{saleId:guid}")]
    public async Task<IActionResult> GetBySale(Guid saleId)
    {
        var result = await _service.GetBySaleAsync(saleId);
        if (result is null) return NotFound(new { error = "Factura electrónica no encontrada" });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result is null) return NotFound(new { error = "Factura electrónica no encontrada" });
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid companyId, [FromQuery] string? countryCode)
    {
        var results = await _service.GetByCompanyAsync(companyId, countryCode);
        return Ok(results);
    }

    [HttpPost("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid id)
    {
        try
        {
            var result = await _service.ResubmitAsync(id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelInvoiceRequest request)
    {
        try
        {
            await _service.CancelAsync(id, request.Reason);
            return Ok(new { message = "Factura anulada exitosamente" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{saleId:guid}/xml")]
    public async Task<IActionResult> GetXml(Guid saleId, [FromQuery] string countryCode)
    {
        try
        {
            var xml = await _service.GenerateXmlAsync(saleId, countryCode);
            return Content(xml, "application/xml");
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> GetPdf(Guid id)
    {
        try
        {
            var url = await _service.GeneratePdfAsync(id);
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
