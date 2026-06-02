using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Application.DTOs.Commercial;
using Nexora.Application.Services;
using Nexora.Web.Filters;

namespace Nexora.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly ClientService _service;

    public ClientsController(ClientService service)
    {
        _service = service;
    }

    [Audit("Client", "Create")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _service.GetByIdAsync(id);
        if (client is null)
            return NotFound(new { error = "Client not found" });
        return Ok(client);
    }

    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id)
    {
        var statement = await _service.GetStatementAsync(id);
        if (statement is null)
            return NotFound(new { error = "Client not found" });
        return Ok(statement);
    }

    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ClientFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Client", "Update")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var client = await _service.UpdateAsync(id, request);
        if (client is null)
            return NotFound(new { error = "Client not found" });
        return Ok(client);
    }

    [Audit("Client", "Delete")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Client not found" });
        return NoContent();
    }
}
