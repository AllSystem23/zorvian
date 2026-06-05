using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/supplier-payments")]
public sealed class SupplierPaymentsController : ControllerBase
{
    private readonly SupplierPaymentService _service;

    public SupplierPaymentsController(SupplierPaymentService service)
    {
        _service = service;
    }

    [Audit("SupplierPayment", "Create")]
    [HttpPost]
    public async Task<IActionResult> RegisterPayment([FromBody] CreateSupplierPaymentRequest request)
    {
        try
        {
            var payment = await _service.RegisterPaymentAsync(request);
            return Ok(payment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("by-purchase/{purchaseId:guid}")]
    public async Task<IActionResult> GetByPurchaseId(Guid purchaseId)
    {
        var payments = await _service.GetByPurchaseIdAsync(purchaseId);
        return Ok(payments);
    }
}
