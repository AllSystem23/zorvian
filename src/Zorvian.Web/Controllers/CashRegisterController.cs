using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.CashRegister;
using Zorvian.Application.Services;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/cash-registers")]
public sealed class CashRegistersController : ControllerBase
{
    private readonly CashRegisterService _service;

    public CashRegistersController(CashRegisterService service)
    {
        _service = service;
    }

    [Audit("CashRegister", "Create")]
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var register = await _service.GetByIdAsync(id);
        if (register is null)
            return NotFound(new { error = "Cash register not found" });
        return Ok(register);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] CashRegisterFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("CashMovement", "Create")]
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

    [HttpGet("{id:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid id)
    {
        var movements = await _service.GetMovementsAsync(id);
        return Ok(movements);
    }

    [Audit("CashRegister", "Arqueo")]
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
    [HttpGet("{id:guid}/arqueo")]
    public async Task<IActionResult> GetArqueo(Guid id)
    {
        var result = await _service.GetArqueoAsync(id);
        if (result is null)
            return NotFound(new { error = "Arqueo not found" });
        return Ok(result);
    }
}
