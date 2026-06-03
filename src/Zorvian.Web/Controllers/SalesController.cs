using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly SaleService _service;

    public SalesController(SaleService service)
    {
        _service = service;
    }

    [Audit("Sale", "Create")]
    [HttpPost("cash")]
    public async Task<IActionResult> CreateCashSale([FromBody] CreateCashSaleRequest request)
    {
        try
        {
            var sale = await _service.CreateCashSaleAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Audit("Sale", "Create")]
    [HttpPost("credit")]
    public async Task<IActionResult> CreateCreditSale([FromBody] CreateCreditSaleRequest request)
    {
        try
        {
            var sale = await _service.CreateCreditSaleAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sale = await _service.GetByIdAsync(id);
        if (sale is null)
            return NotFound(new { error = "Sale not found" });
        return Ok(sale);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] SaleFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }
}
