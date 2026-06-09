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

    public async Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(string countryCode, Guid companyId)
    {
        return await _context.RegionalTaxConfigurations
            .Where(x => x.CountryCode == countryCode && x.CompanyId == companyId && x.IsActive)
            .ToListAsync();
    }

    public async Task AddOrUpdateAsync(RegionalTaxConfiguration taxConfig)
    {
        var existing = await _context.RegionalTaxConfigurations
            .FirstOrDefaultAsync(x => x.CountryCode == taxConfig.CountryCode && x.TaxType == taxConfig.TaxType && x.CompanyId == taxConfig.CompanyId);

        if (existing != null)
        {
            existing.Rate = taxConfig.Rate;
            existing.EffectiveDate = taxConfig.EffectiveDate;
            existing.IsActive = taxConfig.IsActive;
        }
        else
        {
            await _context.RegionalTaxConfigurations.AddAsync(taxConfig);
        }
        await _context.SaveChangesAsync();
    }
}
