using Microsoft.EntityFrameworkCore;
using Nexora.Application.Interfaces;
using Nexora.Core.Entities;
using Nexora.Infrastructure.Data;

namespace Nexora.Infrastructure.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly NexoraDbContext _db;

    public CompanyRepository(NexoraDbContext db)
    {
        _db = db;
    }

    public async Task<Company?> GetByIdAsync(Guid id) =>
        await _db.Companies.FindAsync(id);

    public async Task<Company?> GetByTenantIdAsync(string tenantId) =>
        await _db.Companies.FirstOrDefaultAsync(c => c.TenantId == tenantId);

    public async Task AddAsync(Company company) =>
        await _db.Companies.AddAsync(company);

    public async Task AddSettingsAsync(CompanySettings settings) =>
        await _db.CompanySettings.AddAsync(settings);

    public async Task<CompanySettings?> GetSettingsAsync(Guid companyId) =>
        await _db.CompanySettings.FirstOrDefaultAsync(s => s.CompanyId == companyId);

    public async Task UpdateAsync(Company company)
    {
        _db.Companies.Update(company);
        await Task.CompletedTask;
    }

    public async Task UpdateSettingsAsync(CompanySettings settings)
    {
        _db.CompanySettings.Update(settings);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsByTenantIdAsync(string tenantId) =>
        await _db.Companies.AnyAsync(c => c.TenantId == tenantId);

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
