using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Commercial;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;
using Zorvian.Web.Filters;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly ClientService _service;

    public ClientsController(ClientService service)
    {
        _service = service;
    }

    [Audit("Client", "Create")]
    [RequirePermission(Permissions.ClientWrite)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    [Audit("Client", "Read")]
    [RequirePermission(Permissions.ClientRead)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _service.GetByIdAsync(id);
        if (client is null)
            return NotFound(new { error = "Client not found" });
        return Ok(client);
    }

    [Audit("Client", "ReadStatement")]
    [RequirePermission(Permissions.ClientRead)]
    [HttpGet("{id:guid}/statement")]
    public async Task<IActionResult> GetStatement(Guid id)
    {
        var statement = await _service.GetStatementAsync(id);
        if (statement is null)
            return NotFound(new { error = "Client not found" });
        return Ok(statement);
    }

    [Audit("Client", "ReadList")]
    [RequirePermission(Permissions.ClientRead)]
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] ClientFilterRequest filter)
    {
        var result = await _service.GetFilteredAsync(filter);
        return Ok(result);
    }

    [Audit("Client", "Update")]
    [RequirePermission(Permissions.ClientWrite)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest request)
    {
        var client = await _service.UpdateAsync(id, request);
        if (client is null)
            return NotFound(new { error = "Client not found" });
        return Ok(client);
    }

    [Audit("Client", "Delete")]
    [RequirePermission(Permissions.ClientWrite)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { error = "Client not found" });
        return NoContent();
    }
}
