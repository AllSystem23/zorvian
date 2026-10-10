using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Core.Interfaces;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ZorvianDbContext _db;
    private readonly ITenantContext _tenant;

    public CompanyRepository(ZorvianDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // Contexto de fondo (jobs/consumers, tenant en GUID-cero) o SuperAdmin sin
    // selección → se omite el query filter (mismo patrón que AuthRepository).
    private bool NeedsBypass =>
        _tenant.TenantId is null || _tenant.TenantId.Value == Guid.Empty || _tenant.BypassTenantFilter;

    public async Task<Company?> GetByIdAsync(Guid id) =>
        NeedsBypass
            ? await _db.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id)
            : await _db.Companies.FindAsync(id);

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

    public async Task<List<Company>> GetAllAsync() =>
        await _db.Companies.IgnoreQueryFilters().OrderByDescending(c => c.CreatedAt).ToListAsync();

    public async Task<int> CountActiveAsync() =>
        await _db.Companies.IgnoreQueryFilters().CountAsync(c => c.IsActive);

    public async Task<Company?> GetFirstActiveAsync() =>
        await _db.Companies.Where(c => c.IsActive).OrderBy(c => c.CreatedAt).FirstOrDefaultAsync();

    public async Task SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
