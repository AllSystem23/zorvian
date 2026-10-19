using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Infrastructure.Services;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Clasificación ML de gastos por descripción (función de lectura pura). La
/// consumen el dashboard comercial de BI (kpi.read) y la página de predicciones
/// de ventas (roles que incluyen Employee), cuyo permiso común es `sale.read`;
/// con `kpi.read` la página de predicciones devolvería 403 a un Employee.
/// </summary>
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
    [RequirePermission(Permissions.SaleRead)]
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
