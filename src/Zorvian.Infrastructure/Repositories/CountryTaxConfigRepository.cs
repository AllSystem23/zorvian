using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CountryTaxConfigRepository : ICountryTaxConfigRepository
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public CountryTaxConfigRepository(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Tenant del contexto actual. Vacío o GUID-cero (jobs globales, arranque,
    /// SuperAdmin sin compañía) = catálogo global.
    /// </summary>
    private static string NormalizeTenant(string? raw) =>
        CountryTaxConfig.IsGlobalTenant(raw) ? CountryTaxConfig.GlobalTenantId : raw!;

    private string CurrentTenantId => NormalizeTenant(_tenant.TenantId?.ToString());

    /// <summary>Hay una compañía concreta en el contexto (no SuperAdmin sin selección).</summary>
    private bool HasCompanyScope => CurrentTenantId != CountryTaxConfig.GlobalTenantId;

    public async Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode)
    {
        var code = NormalizeCode(countryCode);
        if (code.Length == 0) return null;

        var tenantId = CurrentTenantId;

        // Una sola consulta: filas globales (de respaldo) + el ajuste de la compañía,
        // con el ajuste propio primero. Las filas de OTRAS compañías nunca entran.
        var query = _db.CountryTaxConfigs
            .Where(c => c.CountryCode == code && c.IsActive)
            .Where(c => c.TenantId == CountryTaxConfig.GlobalTenantId
                        || c.TenantId == string.Empty
                        || c.TenantId == tenantId);

        if (!HasCompanyScope)
            return await query.FirstOrDefaultAsync();

        return await query
            .OrderBy(c => c.TenantId == tenantId ? 0 : 1)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CountryTaxConfig>> GetAllAsync() =>
        await _db.CountryTaxConfigs
            .Where(c => c.TenantId == CountryTaxConfig.GlobalTenantId || c.TenantId == string.Empty)
            .OrderByDescending(c => c.CountryCode)
            .ToListAsync();

    public async Task<CountryTaxConfig?> GetByIdAsync(Guid id) =>
        await _db.CountryTaxConfigs.FirstOrDefaultAsync(c =>
            c.Id == id &&
            (c.TenantId == CountryTaxConfig.GlobalTenantId || c.TenantId == string.Empty));

    public async Task<CountryTaxConfig?> GetGlobalByCountryCodeAsync(string countryCode)
    {
        var code = NormalizeCode(countryCode);
        if (code.Length == 0) return null;

        return await _db.CountryTaxConfigs.FirstOrDefaultAsync(c =>
            c.CountryCode == code &&
            (c.TenantId == CountryTaxConfig.GlobalTenantId || c.TenantId == string.Empty));
    }

    public async Task<CountryTaxConfig?> GetTenantOverrideAsync(string countryCode, string tenantId)
    {
        var code = NormalizeCode(countryCode);
        var scope = NormalizeTenant(tenantId);
        if (code.Length == 0 || scope == CountryTaxConfig.GlobalTenantId) return null;

        return await _db.CountryTaxConfigs.FirstOrDefaultAsync(c =>
            c.CountryCode == code && c.TenantId == scope);
    }

    public async Task AddAsync(CountryTaxConfig entity) => await _db.CountryTaxConfigs.AddAsync(entity);

    public Task UpdateAsync(CountryTaxConfig entity)
    {
        _db.CountryTaxConfigs.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(CountryTaxConfig entity)
    {
        _db.CountryTaxConfigs.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByCountryCodeAsync(string countryCode, Guid? excludeId = null)
    {
        var code = NormalizeCode(countryCode);

        return await _db.CountryTaxConfigs.AnyAsync(c =>
            c.CountryCode == code &&
            (c.TenantId == CountryTaxConfig.GlobalTenantId || c.TenantId == string.Empty) &&
            (excludeId == null || c.Id != excludeId));
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();

    private static string NormalizeCode(string? countryCode) =>
        (countryCode ?? string.Empty).Trim().ToUpperInvariant();
}
