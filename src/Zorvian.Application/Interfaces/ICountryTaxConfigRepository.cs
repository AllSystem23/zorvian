using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

/// <summary>
/// Acceso a la configuración fiscal por país.
///
/// La tabla mezcla dos tipos de fila, distinguidos por <see cref="BaseEntity.TenantId"/>:
/// <list type="bullet">
///   <item><b>Catálogo global</b> (<c>TenantId == CountryTaxConfig.GlobalTenantId</c>,
///   una por país, la administra el SuperAdmin). Es el respaldo.</item>
///   <item><b>Ajuste de compañía</b> (<c>TenantId ==</c> tenant real): el país
///   configurado a la medida de ESA compañía. Gana sobre el catálogo.</item>
/// </list>
/// </summary>
public interface ICountryTaxConfigRepository
{
    /// <summary>
    /// Configuración EFECTIVA del país para el contexto actual: el ajuste de la
    /// compañía si existe y está activo, si no el catálogo global. Es el método que
    /// deben usar nómina, liquidación, vacaciones y documentos.
    /// </summary>
    Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode);

    /// <summary>Catálogo global (sin ajustes de compañía). Alimenta la pantalla del SuperAdmin.</summary>
    Task<List<CountryTaxConfig>> GetAllAsync();

    /// <summary>Fila del catálogo global por Id (las filas de compañía no son editables desde el catálogo).</summary>
    Task<CountryTaxConfig?> GetByIdAsync(Guid id);

    /// <summary>Fila del catálogo global por código ISO (respaldo de un ajuste de compañía).</summary>
    Task<CountryTaxConfig?> GetGlobalByCountryCodeAsync(string countryCode);

    /// <summary>Ajuste propio de [tenantId] para el país, si existe.</summary>
    Task<CountryTaxConfig?> GetTenantOverrideAsync(string countryCode, string tenantId);

    Task AddAsync(CountryTaxConfig entity);
    Task UpdateAsync(CountryTaxConfig entity);
    Task DeleteAsync(CountryTaxConfig entity);

    /// <summary>
    /// ¿Existe ya una fila del catálogo global para el país? Los ajustes de compañía
    /// NO cuentan: cada compañía puede ajustar un país que ya existe en el catálogo.
    /// </summary>
    Task<bool> ExistsByCountryCodeAsync(string countryCode, Guid? excludeId = null);

    Task<int> SaveChangesAsync();
}
