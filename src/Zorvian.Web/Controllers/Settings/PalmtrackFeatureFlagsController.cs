using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Web.Authorization;

namespace Zorvian.Web.Controllers.Settings;

[ApiController]
[Authorize]
[Route("zorvian/v1/settings/palmtrack")]
public sealed class PalmtrackFeatureFlagsController : ControllerBase
{
    private readonly ICompanyRepository _companyRepo;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _configuration;

    public PalmtrackFeatureFlagsController(
        ICompanyRepository companyRepo,
        ITenantContext tenant,
        IConfiguration configuration)
    {
        _companyRepo = companyRepo;
        _tenant = tenant;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /zorvian/v1/settings/palmtrack/feature-flags
    /// Retorna los feature flags de PalmTrack. La fuente de verdad es la fila
    /// CompanySettings de la empresa actual; si no existe (empresa sin configurar),
    /// se hace fallback a la configuración appsettings.json (PalmTrack:*).
    /// </summary>
    [HttpGet("feature-flags")]
    [RequirePermission(Permissions.SettingsRead)]
    public async Task<IActionResult> GetFeatureFlags()
    {
        var settings = await GetSettingsOrDefaultAsync();

        return Ok(new
        {
            moduleEnabled = settings?.PalmTrackEnabled ?? GetBool("PalmTrack:Enabled"),
            ssoEnabled = settings?.PalmTrackSsoEnabled ?? GetBool("PalmTrack:SsoEnabled"),
            ssoAutoCreateUsers = settings?.PalmTrackSsoAutoCreateUsers ?? GetBool("PalmTrack:SsoAutoCreateUsers"),
            ssoPropagateRoles = settings?.PalmTrackSsoPropagateRoles ?? GetBool("PalmTrack:SsoPropagateRoles"),
            ssoSharedProject = settings?.PalmTrackSsoSharedProject ?? GetBool("PalmTrack:SsoSharedProject"),
        });
    }

    /// <summary>
    /// PUT /zorvian/v1/settings/palmtrack/feature-flags
    /// Persiste los feature flags de PalmTrack en la fila CompanySettings de la
    /// empresa actual (se crea si aún no existe), de modo que los cambios
    /// sobrevivan a reinicios y recargas de la página.
    /// </summary>
    [HttpPut("feature-flags")]
    [RequirePermission(Permissions.SettingsWrite)]
    public async Task<IActionResult> UpdateFeatureFlags([FromBody] UpdateFeatureFlagsRequest request)
    {
        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null)
            return NotFound(new { error = "company_not_found", message = "No company found for the current tenant" });

        var settings = await _companyRepo.GetSettingsAsync(company.Id);
        var isNew = settings is null;
        if (isNew)
        {
            settings = new CompanySettings
            {
                CompanyId = company.Id,
                TenantId = company.TenantId,
            };
            await _companyRepo.AddSettingsAsync(settings);
        }

        if (request.ModuleEnabled is not null)
            settings.PalmTrackEnabled = request.ModuleEnabled.Value;

        if (request.SsoEnabled is not null)
            settings.PalmTrackSsoEnabled = request.SsoEnabled.Value;

        if (request.SsoAutoCreateUsers is not null)
            settings.PalmTrackSsoAutoCreateUsers = request.SsoAutoCreateUsers.Value;

        if (request.SsoPropagateRoles is not null)
            settings.PalmTrackSsoPropagateRoles = request.SsoPropagateRoles.Value;

        if (request.SsoSharedProject is not null)
            settings.PalmTrackSsoSharedProject = request.SsoSharedProject.Value;

        // Solo se marca Update cuando la fila ya existía: para una fila recién
        // agregada, Update() la movería de Added a Modified y rompería el guardado.
        if (!isNew)
            await _companyRepo.UpdateSettingsAsync(settings);
        await _companyRepo.SaveChangesAsync();

        // Return updated flags
        return Ok(new
        {
            moduleEnabled = settings.PalmTrackEnabled,
            ssoEnabled = settings.PalmTrackSsoEnabled,
            ssoAutoCreateUsers = settings.PalmTrackSsoAutoCreateUsers,
            ssoPropagateRoles = settings.PalmTrackSsoPropagateRoles,
            ssoSharedProject = settings.PalmTrackSsoSharedProject,
        });
    }

    private async Task<CompanySettings?> GetSettingsOrDefaultAsync()
    {
        var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
        if (company is null) return null;
        return await _companyRepo.GetSettingsAsync(company.Id);
    }

    private bool GetBool(string key)
    {
        var value = _configuration[key];
        if (bool.TryParse(value, out var result))
            return result;
        return false;
    }
}

/// <summary>
/// Request body para actualizar feature flags de PalmTrack.
/// </summary>
public sealed record UpdateFeatureFlagsRequest(
    bool? ModuleEnabled,
    bool? SsoEnabled,
    bool? SsoAutoCreateUsers,
    bool? SsoPropagateRoles,
    bool? SsoSharedProject
);