using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class ReconciliationController : ControllerBase
{
    private readonly IReconciliationService _service;

    public ReconciliationController(IReconciliationService service) => _service = service;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");
        
        using var stream = file.OpenReadStream();
        var result = await _service.ProcessBankResponseFileAsync(stream);
        
        return Ok(result);
    }
}
