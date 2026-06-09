using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.MultiCurrency;
using Zorvian.Application.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Route("zorvian/v1/exchange-rates")]
[Authorize]
public sealed class ExchangeRatesController : ControllerBase
{
    private readonly IExchangeRateService _service;

    public ExchangeRatesController(IExchangeRateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExchangeRateResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExchangeRateResponse>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ExchangeRateResponse>> Create(CreateExchangeRateRequest request)
    {
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ExchangeRateResponse>> Update(Guid id, UpdateExchangeRateRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("rate")]
    public async Task<ActionResult<decimal?>> GetRate(
        [FromQuery] string from,
        [FromQuery] string to,
        [FromQuery] DateTime? date = null)
    {
        var rate = await _service.GetRateAsync(from, to, date);
        return Ok(new { from, to, rate, date });
    }
}
