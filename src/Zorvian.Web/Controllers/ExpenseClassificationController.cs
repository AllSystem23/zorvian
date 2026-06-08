using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Infrastructure.Services;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/expense-classification")]
public sealed class ExpenseClassificationController : ControllerBase
{
    private readonly ExpenseClassificationService _service;

    public ExpenseClassificationController(ExpenseClassificationService service)
    {
        _service = service;
    }

    [HttpGet("predict")]
    public IActionResult Predict([FromQuery] string description, [FromQuery] decimal amount = 0)
    {
        try
        {
            var result = _service.Predict(description, amount);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
