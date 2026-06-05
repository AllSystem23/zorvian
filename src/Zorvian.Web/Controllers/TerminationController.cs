using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class TerminationController : ControllerBase
{
    private readonly TerminationService _service;

    public TerminationController(TerminationService service) => _service = service;

    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(Guid employeeId, TerminationReason reason, DateOnly terminationDate) =>
        Ok(await _service.CalculateAsync(employeeId, reason, terminationDate));
}
