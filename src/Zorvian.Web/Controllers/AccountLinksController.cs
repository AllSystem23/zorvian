using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/account-links")]
public sealed class AccountLinksController : ControllerBase
{
    private readonly AccountLinkService _service;
    public AccountLinksController(AccountLinkService service) => _service = service;

    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Create([FromBody] CreateAccountLinkRequest request)
    {
        var link = await _service.CreateAsync(request);
        return Ok(link);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("seed")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Seed()
    {
        await _service.SeedDefaultLinksAsync();
        return Ok(new { message = "Default account links seeded" });
    }
}
