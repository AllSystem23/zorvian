using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.DTOs.Payroll;
using Zorvian.Application.Services;
using Zorvian.Core.Entities;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

[ApiController]
[Authorize]
[Route("zorvian/v1/[controller]")]
public sealed class TerminationController : ControllerBase
{
    private readonly TerminationService _service;

    public TerminationController(TerminationService service) => _service = service;

    [RequirePermission(Permissions.EmployeeWrite)]
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(
        Guid employeeId,
        TerminationReason reason,
        DateOnly terminationDate,
        DateOnly? paidThroughDate = null,
        decimal overtimeHours = 0,
        decimal overtimePay = 0) =>
        Ok(await _service.CalculateAsync(employeeId, reason, terminationDate, paidThroughDate, overtimeHours, overtimePay));
}
