using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Accounting;
using Zorvian.Application.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly AccountService _service;
    public AccountsController(AccountService service) => _service = service;

    [HttpGet]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetTreeAsync());

    [HttpGet("{id:guid}")]
    [RequirePermission(Permissions.AccountingRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var account = await _service.GetTreeAsync().ContinueWith(t => t.Result.FirstOrDefault(a => a.Id == id));
        if (account is null) return NotFound(new { error = "Account not found" });
        return Ok(account);
    }

    [HttpPost]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request)
    {
        try
        {
            var account = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAccountRequest request)
    {
        try
        {
            var account = await _service.UpdateAsync(id, request);
            return Ok(account);
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("seed")]
    [RequirePermission(Permissions.AccountingWrite)]
    public async Task<IActionResult> Seed()
    {
        await _service.SeedDefaultChartOfAccountsAsync();
        return Ok(new { message = "Chart of accounts seeded" });
    }
}
