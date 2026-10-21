using Zorvian.Core.Entities;

namespace Zorvian.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id);
    /// <summary>
    /// Lookup SIN filtro de compañía — solo para acciones de plataforma
    /// (SuperAdmin revisando solicitudes de acceso, por ejemplo).
    /// </summary>
    Task<Company?> GetByIdForPlatformAsync(Guid id);
    Task<Company?> GetByTenantIdAsync(string tenantId);
    Task AddAsync(Company company);
    Task AddSettingsAsync(CompanySettings settings);
    Task<CompanySettings?> GetSettingsAsync(Guid companyId);
    Task UpdateAsync(Company company);
    Task UpdateSettingsAsync(CompanySettings settings);
    Task<bool> ExistsByTenantIdAsync(string tenantId);
    Task<List<Company>> GetAllAsync();
    Task<int> CountActiveAsync();
    Task<Company?> GetFirstActiveAsync();
    Task SaveChangesAsync();
}
