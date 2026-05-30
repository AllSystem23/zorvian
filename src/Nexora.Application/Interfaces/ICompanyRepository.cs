using Nexora.Core.Entities;

namespace Nexora.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company?> GetByTenantIdAsync(string tenantId);
    Task AddAsync(Company company);
    Task AddSettingsAsync(CompanySettings settings);
    Task<CompanySettings?> GetSettingsAsync(Guid companyId);
    Task UpdateAsync(Company company);
    Task UpdateSettingsAsync(CompanySettings settings);
    Task<bool> ExistsByTenantIdAsync(string tenantId);
    Task SaveChangesAsync();
}
