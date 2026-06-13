using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Infrastructure.Services;
using Zorvian.Core.Interfaces;

namespace Zorvian.Web.Controllers;

/// <summary>
/// Controlador de siembra de datos. Permite poblar la base de datos con datos de prueba.
/// </summary>
[ApiController]
[Authorize]
[Route("zorvian/v1/seed")]
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
    public async Task<IActionResult> Seed([FromBody] SeedCompanyRequest request)
    {
        var tenantId = await _seed.SeedAsync(_tenant.TenantId, request.Name, request.Country, request.TaxId, request.IsStrictlyPrivate);
        return Ok(new { 
            message = $"Empresa '{request.Name}' inicializada correctamente",
            tenantId = tenantId,
            instructions = request.IsStrictlyPrivate 
                ? "Esta empresa es PRIVADA. El Super Admin no tiene acceso directo." 
                : "Si eres Super Admin, ahora puedes cambiar a esta empresa usando el endpoint switch-tenant."
        });
    }

    public sealed record SeedCompanyRequest(string Name, string Country, string TaxId, bool IsStrictlyPrivate = false);

    /// <summary>
    /// Ejecuta la siembra específica para Tienda Brizuela Romero.
    /// </summary>
    [HttpPost("brizuela-romero")]
    public async Task<IActionResult> SeedBrizuela()
    {
        if (!Guid.TryParse(_tenant.TenantId, out var companyId))
            return BadRequest(new { error = "Invalid company context" });

        await _seed.SeedBrizuelaRomeroAsync(companyId);
        return Ok(new { message = "Catálogo y reglas de Tienda Brizuela Romero cargados exitosamente" });
    }

    /// <summary>
    /// Crea el usuario Super Admin en Firebase Authentication y en la base de datos.
    /// Solo accesible cuando no existe ningún Super Admin en el sistema.
    /// </summary>
    [HttpPost("super-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateSuperAdmin([FromBody] CreateSuperAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "El email es requerido" });

        var bootstrapKey = Environment.GetEnvironmentVariable("ZORVIAN_BOOTSTRAP_KEY");
        if (string.IsNullOrEmpty(bootstrapKey) || bootstrapKey != request.BootstrapKey)
            return Unauthorized(new { error = "Bootstrap key inválida o no configurada" });

        var result = await _seed.SeedSuperAdminAsync(request.Email.Trim());

        if (result.AlreadyExists)
            return Ok(new { message = result.PasswordOrMessage });

        return Ok(new
        {
            message = "Super Admin creado exitosamente",
            email = result.Email,
            password = result.PasswordOrMessage,
            tip = "Guarda esta contraseña. No podrás recuperarla después.",
        });
    }
}

public sealed record CreateSuperAdminRequest(string Email, string? BootstrapKey = null);
