using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Infrastructure.Repositories;

public sealed class RegionalTaxConfigurationRepository : IRegionalTaxConfigurationRepository
{
    private readonly ZorvianDbContext _context;

    public RegionalTaxConfigurationRepository(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RegionalTaxConfiguration>> GetAllAsync(Guid companyId)
    {
        return await _context.RegionalTaxConfigurations
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.CountryCode)
            .ThenBy(x => x.TaxType)
            .ToListAsync();
    }

    public async Task<RegionalTaxConfiguration?> GetByIdAsync(Guid id, Guid companyId)
    {
        return await _context.RegionalTaxConfigurations
            .FirstOrDefaultAsync(x => x.Id == id && x.CompanyId == companyId);
    }

    public async Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId)
    {
        return await _context.RegionalTaxConfigurations
            .Where(x => x.CountryCode == countryCode && x.CompanyId == companyId && x.IsActive)
            .ToListAsync();
    }

    public async Task<RegionalTaxConfiguration?> FindByCompositeKeyAsync(string countryCode, string taxType, Guid companyId)
    {
        return await _context.RegionalTaxConfigurations
            .FirstOrDefaultAsync(x =>
                x.CountryCode == countryCode &&
                x.TaxType == taxType &&
                x.CompanyId == companyId);
    }

    public async Task AddAsync(RegionalTaxConfiguration taxConfig)
    {
        await _context.RegionalTaxConfigurations.AddAsync(taxConfig);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(RegionalTaxConfiguration taxConfig)
    {
        _context.RegionalTaxConfigurations.Remove(taxConfig);
        await _context.SaveChangesAsync();
    }
}
