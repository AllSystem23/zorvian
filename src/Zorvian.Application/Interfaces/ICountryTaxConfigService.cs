using Zorvian.Application.DTOs.Tax;

namespace Zorvian.Application.Interfaces;

public interface ICountryTaxConfigService
{
    /// <summary>Catálogo global (SuperAdmin).</summary>
    Task<List<CountryTaxConfigResponse>> GetAllAsync();

    /// <summary>Fila del catálogo global por Id.</summary>
    Task<CountryTaxConfigResponse?> GetByIdAsync(Guid id);

    /// <summary>Configuración EFECTIVA del país para el contexto actual (ajuste de compañía → catálogo).</summary>
    Task<CountryTaxConfigResponse?> GetByCountryCodeAsync(string countryCode);

    // ── Catálogo global (SuperAdmin) ──

    Task<CountryTaxConfigResponse> CreateAsync(CreateCountryTaxConfigRequest request);
    Task<CountryTaxConfigResponse?> UpdateAsync(Guid id, UpdateCountryTaxConfigRequest request);
    Task<bool> DeleteAsync(Guid id);

    // ── Ajuste propio de la compañía (CompanyAdmin) ──

    /// <summary>País de la compañía con sus valores efectivos y si son propios o del catálogo.</summary>
    Task<MyCountryTaxConfigResponse?> GetMyCountryAsync();

    /// <summary>Guarda el ajuste de la compañía para su país (crea la fila propia a partir del catálogo).</summary>
    Task<MyCountryTaxConfigResponse> UpsertMyCountryAsync(UpdateCountryTaxConfigRequest request);

    /// <summary>Descarta el ajuste propio: la compañía vuelve a usar el catálogo global.</summary>
    Task<bool> ResetMyCountryAsync(string? countryCode = null);
}
