using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexora.Infrastructure.Services;
using Nexora.Core.Interfaces;

namespace Nexora.Web.Controllers;

/// <summary>
/// Controlador de siembra de datos. Permite poblar la base de datos con datos de prueba.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/seed")]
public sealed class SeedController : ControllerBase
{
    private readonly SeedService _seed;
    private readonly ITenantContext _tenant;

    public SeedController(SeedService seed, ITenantContext tenant)
    {
        _seed = seed;
        _tenant = tenant;
    }

    /// <summary>
    /// Ejecuta la siembra de datos de prueba en la base de datos.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Seed()
    {
        await _seed.SeedAsync(_tenant.TenantId);
        return Ok(new { message = "Seed data created" });
    }
}
