using Microsoft.EntityFrameworkCore;
using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;

namespace Zorvian.Infrastructure.Repositories;

public sealed class CountryTaxConfigRepository : ICountryTaxConfigRepository
{
    private readonly ZorvianDbContext _db;

    public CountryTaxConfigRepository(ZorvianDbContext db) => _db = db;

    public async Task<CountryTaxConfig?> GetByCountryCodeAsync(string countryCode) =>
        await _db.CountryTaxConfigs.FirstOrDefaultAsync(c => c.CountryCode == countryCode && c.IsActive);

    public async Task<List<CountryTaxConfig>> GetAllAsync() =>
        await _db.CountryTaxConfigs.OrderByDescending(c => c.CountryCode).ToListAsync();

    public async Task<CountryTaxConfig?> GetByIdAsync(Guid id) =>
        await _db.CountryTaxConfigs.FindAsync(id);

    public async Task AddAsync(CountryTaxConfig entity) =>
        await _db.CountryTaxConfigs.AddAsync(entity);

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

    public async Task<bool> ExistsByCountryCodeAsync(string countryCode, Guid? excludeId = null) =>
        await _db.CountryTaxConfigs.AnyAsync(c => c.CountryCode == countryCode && c.Id != excludeId);

    public async Task<int> SaveChangesAsync() =>
        await _db.SaveChangesAsync();
}
