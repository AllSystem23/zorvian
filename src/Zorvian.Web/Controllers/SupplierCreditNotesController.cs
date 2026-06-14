using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/supplier-credit-notes")]
public sealed class SupplierCreditNotesController : ControllerBase
{
    private readonly SupplierCreditNoteService _service;

    public SupplierCreditNotesController(SupplierCreditNoteService service)
    {
        _service = service;
    }

    [Audit("SupplierCreditNote", "Create")]
    [RequirePermission(Permissions.PurchaseWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierCreditNoteRequest request)
    {
        try
        {
            var creditNote = await _service.CreateAsync(request);
            return Ok(creditNote);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var creditNote = await _service.GetByIdAsync(id);
        if (creditNote is null)
            return NotFound(new { error = "Credit note not found" });
        return Ok(creditNote);
    }

    [RequirePermission(Permissions.PurchaseRead)]
    [HttpGet("by-purchase/{purchaseId:guid}")]
    public async Task<IActionResult> GetByPurchaseId(Guid purchaseId)
    {
        var notes = await _service.GetByPurchaseIdAsync(purchaseId);
        return Ok(notes);
    }
}
