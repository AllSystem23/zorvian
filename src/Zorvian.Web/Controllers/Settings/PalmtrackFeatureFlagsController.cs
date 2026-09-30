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
    private readonly ITenantContextWriter _tenantWriter;
    private readonly IConfiguration _configuration;

    public PalmtrackFeatureFlagsController(
        ICompanyRepository companyRepo,
        ITenantContext tenant,
        ITenantContextWriter tenantWriter,
        IConfiguration configuration)
    {
        _companyRepo = companyRepo;
        _tenant = tenant;
        _tenantWriter = tenantWriter;
        _configuration = configuration;
    }

    /// <summary>
    /// GET /zorvian/v1/settings/palmtrack/feature-flags
    /// Retorna los feature flags de PalmTrack. La fuente de verdad es la fila
    /// CompanySettings de la empresa actual (para SuperAdmin sin tenant, la primera
    /// empresa activa: la misma que usa la escritura); si no existe (empresa sin
    /// configurar), se hace fallback a la configuración appsettings.json (PalmTrack:*).
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
        var (company, error) = await TryResolveCompanyAsync();
        if (error is not null)
            return error;
        if (company is null)
            return StatusCode(500, new { error = "company_creation_failed", message = "No se pudo resolver o crear una empresa para el tenant actual" });

        var settings = await _companyRepo.GetSettingsAsync(company.Id);
        var isNew = settings is null;
        if (settings is null)
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

    /// <summary>
    /// Resuelve la empresa actual para operaciones de escritura.
    /// - Si el tenant está configurado, busca la empresa por tenant.
    /// - Si el tenant está vacío y el usuario es SuperAdmin, auto-selecciona
    ///   la primera empresa activa o crea una default.
    /// - Si el tenant está vacío y no es SuperAdmin, retorna 401 con instrucciones.
    /// </summary>
    private async Task<(Company?, IActionResult?)> TryResolveCompanyAsync()
    {
        if (HasValidTenant)
        {
            var company = await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);
            if (company is null)
                return (null, NotFound(new { error = "company_not_found", message = "No company found for the current tenant" }));
            return (company, null);
        }

        // Tenant sin empresa: SuperAdmin puede auto-seleccionar o crear una default
        if (_tenant.IsSuperAdmin)
        {
            var company = await GetOrCreateFirstActiveCompanyAsync();
            _tenantWriter.SetTenantId(company.TenantId);
            return (company, null);
        }

        // Usuario sin tenant: instrucciones claras
        return (null, Unauthorized(new
        {
            error = "tenant_not_selected",
            message = "Seleccione una empresa primero: GET /zorvian/v1/auth/tenants y POST /zorvian/v1/auth/switch-tenant",
        }));
    }

    /// <summary>
    /// Resuelve la empresa desde la que se leen los feature flags.
    /// - Tenant configurado → empresa del tenant.
    /// - Tenant vacío + SuperAdmin → primera empresa activa (la misma que usaría
    ///   la escritura), de modo que "guardar → recargar" devuelva lo persistido
    ///   aunque no haya tenant seleccionado en el token.
    /// - Resto → null (el caller hace fallback a appsettings).
    /// </summary>
    private async Task<Company?> ResolveCompanyForReadAsync()
    {
        if (HasValidTenant)
            return await _companyRepo.GetByTenantIdAsync(_tenant.TenantId);

        if (_tenant.IsSuperAdmin)
            return await _companyRepo.GetFirstActiveAsync();

        return null;
    }

    /// <summary>
    /// Devuelve la primera empresa activa; si no existe ninguna, crea una default.
    /// Solo se invoca desde la ruta de escritura del SuperAdmin.
    /// </summary>
    private async Task<Company> GetOrCreateFirstActiveCompanyAsync()
    {
        var first = await _companyRepo.GetFirstActiveAsync();
        if (first is not null)
            return first;

        // No hay empresas en la BD: crear una default
        var company = new Company
        {
            Name = "Empresa Principal",
            LegalName = "Empresa Principal",
            Country = "NIC",
            Currency = "NIO",
            Timezone = "America/Managua",
            IsActive = true,
            TenantId = Guid.NewGuid().ToString(),
        };
        await _companyRepo.AddAsync(company);
        await _companyRepo.SaveChangesAsync();
        return company;
    }

    private bool HasValidTenant =>
        _tenant.TenantId.TryGetCompanyId(out var cid) && cid != Guid.Empty;

    private async Task<CompanySettings?> GetSettingsOrDefaultAsync()
    {
        var company = await ResolveCompanyForReadAsync();
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