using Zorvian.Application.Interfaces;
using Zorvian.Core.Entities;
using Zorvian.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Zorvian.Infrastructure.Repositories;

public sealed class RegionalDashboardRepository : IRegionalDashboardRepository
{
    private readonly ZorvianDbContext _context;

    public RegionalDashboardRepository(ZorvianDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RegionalTaxConfiguration>> GetActiveTaxesAsync(Guid companyId, string countryCode)
    {
        return await _context.RegionalTaxConfigurations
            .Where(x => x.CompanyId == companyId && x.CountryCode == countryCode && x.IsActive)
            .ToListAsync();
    }

    public async Task<decimal> GetPayrollTotalAsync(Guid companyId, string countryCode)
    {
        return await _context.PayrollDetailConcepts
            .Where(x => x.CompanyId == companyId && !x.IsDeleted)
            .SumAsync(x => (decimal?)x.Amount) ?? 0;
    }
}
